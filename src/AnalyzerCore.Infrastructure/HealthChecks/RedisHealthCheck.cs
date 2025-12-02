using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AnalyzerCore.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Redis connectivity.
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(
        IConnectionMultiplexer? redis,
        ILogger<RedisHealthCheck> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_redis is null)
        {
            return HealthCheckResult.Healthy("Redis is not configured (using in-memory cache)");
        }

        try
        {
            var database = _redis.GetDatabase();
            var pingTime = await database.PingAsync();

            var data = new Dictionary<string, object>
            {
                { "ping_ms", pingTime.TotalMilliseconds },
                { "is_connected", _redis.IsConnected },
                { "configuration", _redis.Configuration }
            };

            // Get server info
            foreach (var endpoint in _redis.GetEndPoints())
            {
                var server = _redis.GetServer(endpoint);
                if (server.IsConnected)
                {
                    data[$"server_{endpoint}"] = new
                    {
                        is_replica = server.IsReplica,
                        server_type = server.ServerType.ToString()
                    };
                }
            }

            if (pingTime.TotalMilliseconds > 100)
            {
                return HealthCheckResult.Degraded(
                    $"Redis responding slowly: {pingTime.TotalMilliseconds:F2}ms",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Redis is healthy. Ping: {pingTime.TotalMilliseconds:F2}ms",
                data: data);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(ex, "Redis connection failed during health check");
            return HealthCheckResult.Unhealthy(
                "Redis connection failed",
                ex,
                new Dictionary<string, object>
                {
                    { "exception_type", ex.GetType().Name },
                    { "message", ex.Message }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy(
                "Redis health check failed",
                ex);
        }
    }
}
