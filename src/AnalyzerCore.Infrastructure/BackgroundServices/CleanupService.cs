using AnalyzerCore.Infrastructure.Configuration;
using AnalyzerCore.Infrastructure.Persistence;
using AnalyzerCore.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalyzerCore.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that periodically cleans up old outbox messages and idempotent requests.
/// This prevents the database from growing unbounded over time.
/// </summary>
public sealed class CleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CleanupService> _logger;
    private readonly CleanupOptions _options;
    private readonly ApplicationMetrics? _metrics;

    public CleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<CleanupService> logger,
        IOptions<CleanupOptions> options,
        ApplicationMetrics? metrics = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Cleanup service starting. Interval: {IntervalMinutes} minutes, " +
            "Outbox retention: {OutboxDays} days, Idempotency retention: {IdempotencyDays} days",
            _options.IntervalMinutes,
            _options.OutboxRetentionDays,
            _options.IdempotencyRetentionDays);

        // Initial delay to let the application start up properly
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup run");
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Cleanup service stopping");
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting cleanup run");

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var outboxDeleted = await CleanupOutboxMessagesAsync(dbContext, cancellationToken);
        var idempotencyDeleted = await CleanupIdempotentRequestsAsync(dbContext, cancellationToken);

        _logger.LogInformation(
            "Cleanup completed. Deleted {OutboxCount} outbox messages and {IdempotencyCount} idempotent requests",
            outboxDeleted,
            idempotencyDeleted);
    }

    private async Task<int> CleanupOutboxMessagesAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_options.OutboxRetentionDays);
        var totalDeleted = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Find messages to delete
            var messagesToDelete = await dbContext.OutboxMessages
                .Where(m => m.ProcessedOnUtc != null && m.ProcessedOnUtc < cutoffDate)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);

            // Optionally include failed messages
            if (_options.DeleteFailedMessages)
            {
                var failedMessages = await dbContext.OutboxMessages
                    .Where(m => m.Error != null && m.OccurredOnUtc < cutoffDate)
                    .Take(_options.BatchSize - messagesToDelete.Count)
                    .ToListAsync(cancellationToken);

                messagesToDelete.AddRange(failedMessages);
            }

            if (!messagesToDelete.Any())
            {
                break;
            }

            dbContext.OutboxMessages.RemoveRange(messagesToDelete);
            await dbContext.SaveChangesAsync(cancellationToken);

            totalDeleted += messagesToDelete.Count;

            _logger.LogDebug("Deleted batch of {Count} outbox messages", messagesToDelete.Count);

            // Avoid tight loop
            await Task.Delay(100, cancellationToken);
        }

        // Update pending count metric
        var pendingCount = await dbContext.OutboxMessages
            .CountAsync(m => m.ProcessedOnUtc == null, cancellationToken);
        _metrics?.SetOutboxPendingCount(pendingCount);

        return totalDeleted;
    }

    private async Task<int> CleanupIdempotentRequestsAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_options.IdempotencyRetentionDays);
        var totalDeleted = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Find requests to delete
            var requestsToDelete = await dbContext.IdempotentRequests
                .Where(r => r.CreatedOnUtc < cutoffDate)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);

            if (!requestsToDelete.Any())
            {
                break;
            }

            dbContext.IdempotentRequests.RemoveRange(requestsToDelete);
            await dbContext.SaveChangesAsync(cancellationToken);

            totalDeleted += requestsToDelete.Count;

            _logger.LogDebug("Deleted batch of {Count} idempotent requests", requestsToDelete.Count);

            // Avoid tight loop
            await Task.Delay(100, cancellationToken);
        }

        return totalDeleted;
    }
}
