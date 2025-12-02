using System.Text.Json;
using AnalyzerCore.Application.Abstractions.Caching;
using AnalyzerCore.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AnalyzerCore.Infrastructure.Caching;

/// <summary>
/// Redis-based cache implementation using StackExchange.Redis.
/// </summary>
public sealed class RedisCacheService : ICacheService, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IOptions<RedisOptions> options,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var prefixedKey = GetPrefixedKey(key);
            var value = await _database.StringGetAsync(prefixedKey);

            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Redis cache miss for key: {CacheKey}", key);
                return null;
            }

            _logger.LogDebug("Redis cache hit for key: {CacheKey}", key);
            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error getting key: {CacheKey}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var prefixedKey = GetPrefixedKey(key);
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            var exp = expiration ?? TimeSpan.FromSeconds(_options.DefaultExpirationSeconds);

            await _database.StringSetAsync(prefixedKey, serialized, exp);

            _logger.LogDebug("Redis cache set for key: {CacheKey}, Expiration: {Expiration}", key, exp);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error setting key: {CacheKey}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var prefixedKey = GetPrefixedKey(key);
            await _database.KeyDeleteAsync(prefixedKey);

            _logger.LogDebug("Redis cache removed for key: {CacheKey}", key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error removing key: {CacheKey}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var prefixedPattern = GetPrefixedKey(prefix) + "*";
            var endpoints = _redis.GetEndPoints();
            var removedCount = 0;

            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                if (server.IsConnected && !server.IsReplica)
                {
                    var keys = server.Keys(pattern: prefixedPattern).ToArray();
                    if (keys.Length > 0)
                    {
                        await _database.KeyDeleteAsync(keys);
                        removedCount += keys.Length;
                    }
                }
            }

            _logger.LogDebug("Redis cache removed {Count} entries with prefix: {Prefix}", removedCount, prefix);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error removing keys by prefix: {Prefix}", prefix);
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

    private string GetPrefixedKey(string key) => $"{_options.InstanceName}{key}";

    public void Dispose()
    {
        if (_disposed) return;

        // Note: We don't dispose the IConnectionMultiplexer here as it's managed by DI
        _disposed = true;
    }
}
