using AnalyzerCore.Domain.Events;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Infrastructure.Configuration;
using AnalyzerCore.Infrastructure.RealTime;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalyzerCore.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that continuously scans for arbitrage opportunities.
/// </summary>
public class ArbitrageScannerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ArbitrageScannerService> _logger;
    private readonly ArbitrageScannerOptions _options;

    // Track detected opportunities to avoid duplicate alerts
    private readonly HashSet<string> _recentOpportunityHashes = new();
    private readonly object _lockObject = new();

    public ArbitrageScannerService(
        IServiceProvider serviceProvider,
        IOptions<ArbitrageScannerOptions> options,
        ILogger<ArbitrageScannerService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Arbitrage scanner is disabled");
            return;
        }

        _logger.LogInformation(
            "Starting arbitrage scanner with {Interval}ms interval, min profit: ${MinProfit}",
            _options.ScanIntervalMs,
            _options.MinProfitUsd);

        // Initial delay to let other services start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanForOpportunitiesAsync(stoppingToken);
                await CleanupOldOpportunitiesAsync();
                await Task.Delay(_options.ScanIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Arbitrage scanner is stopping...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during arbitrage scan. Service will continue...");
                await Task.Delay(_options.ErrorRetryDelayMs, stoppingToken);
            }
        }
    }

    private async Task ScanForOpportunitiesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var arbitrageService = scope.ServiceProvider.GetRequiredService<IArbitrageService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        _logger.LogDebug("Starting arbitrage scan...");

        // Scan for two-pool arbitrage
        var scanResult = await arbitrageService.ScanForOpportunitiesAsync(
            _options.MinProfitUsd,
            stoppingToken);

        if (scanResult.IsFailure)
        {
            _logger.LogWarning("Arbitrage scan failed: {Error}", scanResult.Error.Message);
            return;
        }

        var opportunities = scanResult.Value;
        var newOpportunities = 0;
        var largeOpportunities = 0;

        foreach (var opportunity in opportunities)
        {
            if (stoppingToken.IsCancellationRequested) break;

            var hash = GetOpportunityHash(opportunity);

            // Skip if we've recently seen this opportunity
            if (IsRecentOpportunity(hash)) continue;

            MarkOpportunityAsSeen(hash);
            newOpportunities++;

            // Publish domain event
            await publisher.Publish(new ArbitrageOpportunityDetectedEvent
            {
                OpportunityId = opportunity.Id,
                TokenAddress = opportunity.TokenAddress,
                TokenSymbol = opportunity.TokenSymbol,
                SpreadPercent = opportunity.SpreadPercent,
                ExpectedProfitUsd = opportunity.ExpectedProfitUsd,
                NetProfitUsd = opportunity.NetProfitUsd,
                PathLength = opportunity.Path.Count,
                OccurredAt = DateTime.UtcNow
            }, stoppingToken);

            // Check for large opportunity alert
            if (opportunity.NetProfitUsd >= _options.LargeOpportunityThresholdUsd &&
                opportunity.ConfidenceScore >= _options.MinConfidenceScore)
            {
                largeOpportunities++;

                await publisher.Publish(new LargeArbitrageAlertEvent
                {
                    OpportunityId = opportunity.Id,
                    TokenAddress = opportunity.TokenAddress,
                    TokenSymbol = opportunity.TokenSymbol,
                    NetProfitUsd = opportunity.NetProfitUsd,
                    SpreadPercent = opportunity.SpreadPercent,
                    ConfidenceScore = opportunity.ConfidenceScore,
                    OccurredAt = DateTime.UtcNow
                }, stoppingToken);
            }

            // Broadcast via SignalR
            await notificationService.NotifyArbitrageOpportunityAsync(opportunity);
        }

        if (newOpportunities > 0)
        {
            _logger.LogInformation(
                "Arbitrage scan complete: {Total} opportunities found, {New} new, {Large} large alerts",
                opportunities.Count,
                newOpportunities,
                largeOpportunities);
        }
        else
        {
            _logger.LogDebug("Arbitrage scan complete: no new opportunities");
        }

        // Also scan for triangular arbitrage if enabled
        if (_options.EnableTriangularScan)
        {
            await ScanTriangularArbitrageAsync(arbitrageService, notificationService, publisher, stoppingToken);
        }
    }

    private async Task ScanTriangularArbitrageAsync(
        IArbitrageService arbitrageService,
        IRealtimeNotificationService notificationService,
        IPublisher publisher,
        CancellationToken stoppingToken)
    {
        // Scan triangular arbitrage starting from WETH
        var triangularResult = await arbitrageService.FindTriangularOpportunitiesAsync(
            _options.TriangularBaseToken,
            stoppingToken);

        if (triangularResult.IsFailure)
        {
            _logger.LogDebug("Triangular arbitrage scan failed: {Error}", triangularResult.Error.Message);
            return;
        }

        var triangularOpportunities = triangularResult.Value
            .Where(o => o.NetProfitUsd >= _options.MinProfitUsd)
            .ToList();

        foreach (var opportunity in triangularOpportunities)
        {
            if (stoppingToken.IsCancellationRequested) break;

            var hash = GetOpportunityHash(opportunity);
            if (IsRecentOpportunity(hash)) continue;

            MarkOpportunityAsSeen(hash);

            await publisher.Publish(new ArbitrageOpportunityDetectedEvent
            {
                OpportunityId = opportunity.Id,
                TokenAddress = opportunity.TokenAddress,
                TokenSymbol = opportunity.TokenSymbol,
                SpreadPercent = opportunity.SpreadPercent,
                ExpectedProfitUsd = opportunity.ExpectedProfitUsd,
                NetProfitUsd = opportunity.NetProfitUsd,
                PathLength = opportunity.Path.Count,
                OccurredAt = DateTime.UtcNow
            }, stoppingToken);

            await notificationService.NotifyArbitrageOpportunityAsync(opportunity);
        }

        if (triangularOpportunities.Any())
        {
            _logger.LogInformation(
                "Triangular arbitrage scan: {Count} opportunities found",
                triangularOpportunities.Count);
        }
    }

    private static string GetOpportunityHash(Domain.ValueObjects.ArbitrageOpportunity opportunity)
    {
        // Create a hash based on token, path, and approximate profit range
        var pathKey = string.Join("-", opportunity.Path.Select(p => p.PoolAddress));
        var profitBucket = Math.Floor(opportunity.NetProfitUsd / 10) * 10; // Round to nearest $10
        return $"{opportunity.TokenAddress}:{pathKey}:{profitBucket}";
    }

    private bool IsRecentOpportunity(string hash)
    {
        lock (_lockObject)
        {
            return _recentOpportunityHashes.Contains(hash);
        }
    }

    private void MarkOpportunityAsSeen(string hash)
    {
        lock (_lockObject)
        {
            _recentOpportunityHashes.Add(hash);
        }
    }

    private Task CleanupOldOpportunitiesAsync()
    {
        // Clear old opportunities periodically to allow re-detection
        // This runs every scan cycle, but we only clear if the set is large
        lock (_lockObject)
        {
            if (_recentOpportunityHashes.Count > _options.MaxCachedOpportunities)
            {
                _logger.LogDebug(
                    "Clearing {Count} cached opportunities",
                    _recentOpportunityHashes.Count);
                _recentOpportunityHashes.Clear();
            }
        }
        return Task.CompletedTask;
    }
}
