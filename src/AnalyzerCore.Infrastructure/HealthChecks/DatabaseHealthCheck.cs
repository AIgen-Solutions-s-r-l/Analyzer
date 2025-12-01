using AnalyzerCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Infrastructure.HealthChecks;

/// <summary>
/// Health check for database connectivity.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        ApplicationDbContext dbContext,
        ILogger<DatabaseHealthCheck> logger)
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
            // Check database connectivity
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                _logger.LogWarning("Database health check failed: Cannot connect to database");
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // Get some basic stats
            var tokenCount = await _dbContext.Tokens.CountAsync(cancellationToken);
            var poolCount = await _dbContext.Pools.CountAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "tokenCount", tokenCount },
                { "poolCount", poolCount },
                { "provider", _dbContext.Database.ProviderName ?? "unknown" }
            };

            return HealthCheckResult.Healthy("Database is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed with exception");
            return HealthCheckResult.Unhealthy("Database check failed", ex);
        }
    }
}
