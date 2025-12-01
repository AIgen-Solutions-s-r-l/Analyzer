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
/// Decorator that adds caching to ITokenRepository operations.
/// Follows the Decorator Pattern for transparent caching.
/// </summary>
public sealed class CachedTokenRepository : ITokenRepository
{
    private readonly ITokenRepository _decorated;
    private readonly ICacheService _cache;
    private readonly CachingOptions _options;
    private readonly ILogger<CachedTokenRepository> _logger;

    public CachedTokenRepository(
        ITokenRepository decorated,
        ICacheService cache,
        IOptions<CachingOptions> options,
        ILogger<CachedTokenRepository> logger)
    {
        _decorated = decorated;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Token?> GetByAddressAsync(string address, string chainId, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.Tokens.ByAddress(address, chainId);

        return await _cache.GetOrSetAsync(
            key,
            ct => _decorated.GetByAddressAsync(address, chainId, ct),
            _options.TokenExpiration,
            cancellationToken);
    }

    public async Task<IEnumerable<Token>> GetAllByChainIdAsync(string chainId, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.Tokens.ByChainId(chainId);

        var result = await _cache.GetOrSetAsync(
            key,
            async ct =>
            {
                var tokens = await _decorated.GetAllByChainIdAsync(chainId, ct);
                return new TokenCollection(tokens);
            },
            _options.TokenExpiration,
            cancellationToken);

        return result?.Tokens ?? Array.Empty<Token>();
    }

    public async Task<Token> AddAsync(Token token, CancellationToken cancellationToken = default)
    {
        var result = await _decorated.AddAsync(token, cancellationToken);

        // Invalidate related caches
        await InvalidateTokenCachesAsync(token, cancellationToken);

        _logger.LogDebug("Token added and cache invalidated for address: {Address}", token.Address);

        return result;
    }

    public async Task<bool> ExistsAsync(string address, string chainId, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.Tokens.Exists(address, chainId);

        var cached = await _cache.GetAsync<ExistsCacheEntry>(key, cancellationToken);
        if (cached is not null)
        {
            return cached.Exists;
        }

        var exists = await _decorated.ExistsAsync(address, chainId, cancellationToken);
        await _cache.SetAsync(key, new ExistsCacheEntry(exists), _options.TokenExpiration, cancellationToken);

        return exists;
    }

    public Task<IEnumerable<Token>> GetTokensCreatedAfterAsync(DateTime timestamp, string chainId, CancellationToken cancellationToken = default)
    {
        // Don't cache time-sensitive queries
        return _decorated.GetTokensCreatedAfterAsync(timestamp, chainId, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _decorated.SaveChangesAsync(cancellationToken);
    }

    private async Task InvalidateTokenCachesAsync(Token token, CancellationToken cancellationToken)
    {
        // Invalidate by-chain cache
        await _cache.RemoveAsync(CacheKeys.Tokens.ByChainId(token.ChainId), cancellationToken);
    }

    /// <summary>
    /// Wrapper class for caching collections (IMemoryCache requires reference types).
    /// </summary>
    private sealed class TokenCollection
    {
        public IEnumerable<Token> Tokens { get; }

        public TokenCollection(IEnumerable<Token> tokens)
        {
            Tokens = tokens;
        }
    }

    /// <summary>
    /// Wrapper class for caching boolean values.
    /// </summary>
    private sealed record ExistsCacheEntry(bool Exists);
}
