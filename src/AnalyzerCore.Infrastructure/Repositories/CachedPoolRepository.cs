using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnalyzerCore.Application.Abstractions.Caching;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Infrastructure.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalyzerCore.Infrastructure.Repositories;

/// <summary>
/// Decorator that adds caching to IPoolRepository operations.
/// Follows the Decorator Pattern for transparent caching.
/// </summary>
public sealed class CachedPoolRepository : IPoolRepository
{
    private readonly IPoolRepository _decorated;
    private readonly ICacheService _cache;
    private readonly CachingOptions _options;
    private readonly ILogger<CachedPoolRepository> _logger;

    public CachedPoolRepository(
        IPoolRepository decorated,
        ICacheService cache,
        IOptions<CachingOptions> options,
        ILogger<CachedPoolRepository> logger)
    {
        _decorated = decorated;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Pool?> GetByAddressAsync(string address, string factory, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.Pools.ByAddress(address, factory);

        return await _cache.GetOrSetAsync(
            key,
            ct => _decorated.GetByAddressAsync(address, factory, ct),
            _options.PoolExpiration,
            cancellationToken);
    }

    public async Task<IEnumerable<Pool>> GetAllByFactoryAsync(string factory, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.Pools.ByFactory(factory);

        var result = await _cache.GetOrSetAsync(
            key,
            async ct =>
            {
                var pools = await _decorated.GetAllByFactoryAsync(factory, ct);
                return new PoolCollection(pools);
            },
            _options.PoolExpiration,
            cancellationToken);

        return result?.Pools ?? Array.Empty<Pool>();
    }

    public async Task<IEnumerable<Pool>> GetAllByChainIdAsync(string chainId, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.Pools.ByChainId(chainId);

        var result = await _cache.GetOrSetAsync(
            key,
            async ct =>
            {
                var pools = await _decorated.GetAllByChainIdAsync(chainId, ct);
                return new PoolCollection(pools);
            },
            _options.PoolExpiration,
            cancellationToken);

        return result?.Pools ?? Array.Empty<Pool>();
    }

    public async Task<IEnumerable<Pool>> GetPoolsByTokenAsync(string tokenAddress, string chainId, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.Pools.ByToken(tokenAddress, chainId);

        var result = await _cache.GetOrSetAsync(
            key,
            async ct =>
            {
                var pools = await _decorated.GetPoolsByTokenAsync(tokenAddress, chainId, ct);
                return new PoolCollection(pools);
            },
            _options.PoolExpiration,
            cancellationToken);

        return result?.Pools ?? Array.Empty<Pool>();
    }

    public async Task<Pool> AddAsync(Pool pool, CancellationToken cancellationToken = default)
    {
        var result = await _decorated.AddAsync(pool, cancellationToken);

        // Invalidate related caches
        await InvalidatePoolCachesAsync(pool, cancellationToken);

        _logger.LogDebug("Pool added and cache invalidated for address: {Address}", pool.Address);

        return result;
    }

    public async Task<bool> ExistsAsync(string address, string factory, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.Pools.Exists(address, factory);

        var cached = await _cache.GetAsync<ExistsCacheEntry>(key, cancellationToken);
        if (cached is not null)
        {
            return cached.Exists;
        }

        var exists = await _decorated.ExistsAsync(address, factory, cancellationToken);
        await _cache.SetAsync(key, new ExistsCacheEntry(exists), _options.PoolExpiration, cancellationToken);

        return exists;
    }

    public Task<IEnumerable<Pool>> GetPoolsCreatedAfterAsync(DateTime timestamp, string factory, CancellationToken cancellationToken = default)
    {
        // Don't cache time-sensitive queries
        return _decorated.GetPoolsCreatedAfterAsync(timestamp, factory, cancellationToken);
    }

    public async Task UpdateReservesAsync(string address, string factory, decimal reserve0, decimal reserve1, CancellationToken cancellationToken = default)
    {
        await _decorated.UpdateReservesAsync(address, factory, reserve0, reserve1, cancellationToken);

        // Invalidate specific pool cache
        var key = CacheKeys.Pools.ByAddress(address, factory);
        await _cache.RemoveAsync(key, cancellationToken);

        _logger.LogDebug("Pool reserves updated and cache invalidated for address: {Address}", address);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _decorated.SaveChangesAsync(cancellationToken);
    }

    private async Task InvalidatePoolCachesAsync(Pool pool, CancellationToken cancellationToken)
    {
        // Invalidate by-factory cache
        await _cache.RemoveAsync(CacheKeys.Pools.ByFactory(pool.Factory), cancellationToken);

        // Invalidate by-chain cache
        await _cache.RemoveAsync(CacheKeys.Pools.ByChainId(pool.ChainId), cancellationToken);

        // Invalidate token-specific caches
        await _cache.RemoveAsync(CacheKeys.Pools.ByToken(pool.Token0Address, pool.ChainId), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Pools.ByToken(pool.Token1Address, pool.ChainId), cancellationToken);
    }

    /// <summary>
    /// Wrapper class for caching collections (IMemoryCache requires reference types).
    /// </summary>
    private sealed class PoolCollection
    {
        public IEnumerable<Pool> Pools { get; }

        public PoolCollection(IEnumerable<Pool> pools)
        {
            Pools = pools;
        }
    }

    /// <summary>
    /// Wrapper class for caching boolean values.
    /// </summary>
    private sealed record ExistsCacheEntry(bool Exists);
}
