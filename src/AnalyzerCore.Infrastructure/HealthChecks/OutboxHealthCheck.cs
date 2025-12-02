using AnalyzerCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Outbox message backlog.
/// </summary>
public sealed class OutboxHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<OutboxHealthCheck> _logger;
    private const int WarningThreshold = 100;
    private const int UnhealthyThreshold = 1000;
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(30);

    public OutboxHealthCheck(
        ApplicationDbContext dbContext,
        ILogger<OutboxHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingCount = await _dbContext.OutboxMessages
                .CountAsync(m => m.ProcessedOnUtc == null, cancellationToken);

            var staleCount = await _dbContext.OutboxMessages
                .CountAsync(m =>
                    m.ProcessedOnUtc == null &&
                    m.OccurredOnUtc < DateTime.UtcNow.Subtract(StaleThreshold),
                    cancellationToken);

            var oldestPending = await _dbContext.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null)
                .OrderBy(m => m.OccurredOnUtc)
                .Select(m => m.OccurredOnUtc)
                .FirstOrDefaultAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "pendingCount", pendingCount },
                { "staleCount", staleCount }
            };

            if (oldestPending != default)
            {
                data["oldestPendingAge"] = (DateTime.UtcNow - oldestPending).ToString();
            }

            if (pendingCount >= UnhealthyThreshold)
            {
                _logger.LogError(
                    "Outbox backlog is critically high: {Count} pending messages",
                    pendingCount);

                return HealthCheckResult.Unhealthy(
                    $"Outbox backlog critical: {pendingCount} pending messages",
                    data: data);
            }

            if (pendingCount >= WarningThreshold || staleCount > 0)
            {
                _logger.LogWarning(
                    "Outbox backlog is elevated: {PendingCount} pending, {StaleCount} stale",
                    pendingCount,
                    staleCount);

                return HealthCheckResult.Degraded(
                    $"Outbox backlog elevated: {pendingCount} pending, {staleCount} stale",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Outbox healthy: {pendingCount} pending messages",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Outbox health check failed with exception");
            return HealthCheckResult.Unhealthy("Outbox check failed", ex);
        }
    }
}
