using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Infrastructure.HealthChecks;

/// <summary>
/// Health check for application memory usage.
/// </summary>
public sealed class MemoryHealthCheck : IHealthCheck
{
    private readonly ILogger<MemoryHealthCheck> _logger;
    private const long WarningThresholdMB = 500;
    private const long UnhealthyThresholdMB = 1000;

    public MemoryHealthCheck(ILogger<MemoryHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var memoryInfo = GC.GetGCMemoryInfo();
        var currentMemoryMB = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
        var totalMemoryMB = memoryInfo.TotalAvailableMemoryBytes / 1024 / 1024;
        var gen0Collections = GC.CollectionCount(0);
        var gen1Collections = GC.CollectionCount(1);
        var gen2Collections = GC.CollectionCount(2);

        var data = new Dictionary<string, object>
        {
            { "workingSetMB", currentMemoryMB },
            { "totalAvailableMB", totalMemoryMB },
            { "gen0Collections", gen0Collections },
            { "gen1Collections", gen1Collections },
            { "gen2Collections", gen2Collections }
        };

        if (currentMemoryMB >= UnhealthyThresholdMB)
        {
            _logger.LogError(
                "Memory usage is critically high: {CurrentMB}MB",
                currentMemoryMB);

            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Memory usage critical: {currentMemoryMB}MB",
                data: data));
        }

        if (currentMemoryMB >= WarningThresholdMB)
        {
            _logger.LogWarning(
                "Memory usage is elevated: {CurrentMB}MB",
                currentMemoryMB);

            return Task.FromResult(HealthCheckResult.Degraded(
                $"Memory usage elevated: {currentMemoryMB}MB",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Memory healthy: {currentMemoryMB}MB",
            data));
    }
}

// Helper to avoid namespace conflict
file static class Process
{
    public static System.Diagnostics.Process GetCurrentProcess() =>
        System.Diagnostics.Process.GetCurrentProcess();
}
