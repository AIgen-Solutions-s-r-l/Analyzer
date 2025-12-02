using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Infrastructure.Configuration;

/// <summary>
/// Configuration options for rate limiting RPC calls.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    /// <summary>
    /// Whether rate limiting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of requests per window.
    /// </summary>
    [Range(1, 10000)]
    public int MaxRequestsPerWindow { get; set; } = 100;

    /// <summary>
    /// Window duration in seconds.
    /// </summary>
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 1;

    /// <summary>
    /// Maximum number of concurrent requests.
    /// </summary>
    [Range(1, 100)]
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Timeout for acquiring a rate limit slot in milliseconds.
    /// </summary>
    [Range(100, 60000)]
    public int AcquireTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Whether to queue requests when rate limited (vs. rejecting immediately).
    /// </summary>
    public bool QueueExcessRequests { get; set; } = true;

    /// <summary>
    /// Maximum queue length when queueing is enabled.
    /// </summary>
    [Range(1, 1000)]
    public int MaxQueueLength { get; set; } = 100;
}
