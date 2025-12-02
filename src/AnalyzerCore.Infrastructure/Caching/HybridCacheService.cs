using AnalyzerCore.Application.Abstractions.Caching;
using AnalyzerCore.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalyzerCore.Infrastructure.Caching;

/// <summary>
/// Hybrid cache service that uses Redis as primary and falls back to in-memory cache.
/// Provides resilience when Redis is unavailable.
/// </summary>
public sealed class HybridCacheService : ICacheService
{
    private readonly RedisCacheService _redisCache;
    private readonly InMemoryCacheService _memoryCache;
    private readonly RedisOptions _options;
    private readonly ILogger<HybridCacheService> _logger;

    public HybridCacheService(
        RedisCacheService redisCache,
        InMemoryCacheService memoryCache,
        IOptions<RedisOptions> options,
        ILogger<HybridCacheService> logger)
    {
        _redisCache = redisCache;
        _memoryCache = memoryCache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        // Try L1 (memory) first for fastest access
        var memoryResult = await _memoryCache.GetAsync<T>(key, cancellationToken);
        if (memoryResult is not null)
        {
            return memoryResult;
        }

        // Try L2 (Redis)
        try
        {
            var redisResult = await _redisCache.GetAsync<T>(key, cancellationToken);
            if (redisResult is not null)
            {
                // Populate L1 cache for subsequent reads
                await _memoryCache.SetAsync(key, redisResult, TimeSpan.FromSeconds(60), cancellationToken);
                return redisResult;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache unavailable, returning null for key: {Key}", key);
        }

        return null;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // Set in both caches
        var tasks = new List<Task>
        {
            _memoryCache.SetAsync(key, value, expiration, cancellationToken)
        };

        try
        {
            tasks.Add(_redisCache.SetAsync(key, value, expiration, cancellationToken));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set key in Redis cache: {Key}", key);
            // Memory cache was still updated
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _memoryCache.RemoveAsync(key, cancellationToken)
        };

        try
        {
            tasks.Add(_redisCache.RemoveAsync(key, cancellationToken));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove key from Redis cache: {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _memoryCache.RemoveByPrefixAsync(prefix, cancellationToken)
        };

        try
        {
            tasks.Add(_redisCache.RemoveByPrefixAsync(prefix, cancellationToken));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove keys by prefix from Redis cache: {Prefix}", prefix);
        }
    }

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);
        if (value is not null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }
}
