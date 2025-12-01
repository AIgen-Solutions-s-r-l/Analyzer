using System;
using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Infrastructure.Caching;

/// <summary>
/// Configuration options for caching behavior.
/// </summary>
public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    /// <summary>
    /// Default cache expiration time for pool data.
    /// </summary>
    [Range(1, 3600)]
    public int PoolExpirationSeconds { get; set; } = 300;

    /// <summary>
    /// Default cache expiration time for token data.
    /// </summary>
    [Range(1, 3600)]
    public int TokenExpirationSeconds { get; set; } = 600;

    /// <summary>
    /// Whether caching is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public TimeSpan PoolExpiration => TimeSpan.FromSeconds(PoolExpirationSeconds);
    public TimeSpan TokenExpiration => TimeSpan.FromSeconds(TokenExpirationSeconds);
}
