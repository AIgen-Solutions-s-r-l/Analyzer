using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Infrastructure.Configuration;

/// <summary>
/// Configuration options for blockchain monitoring.
/// </summary>
public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    /// <summary>
    /// Interval between block polling cycles in milliseconds.
    /// </summary>
    [Range(1000, 3600000)]
    public int PollingInterval { get; set; } = 120000;

    /// <summary>
    /// Number of blocks to process per polling cycle.
    /// </summary>
    [Range(1, 1000)]
    public int BlocksToProcess { get; set; } = 5;

    /// <summary>
    /// Number of blocks per RPC batch request.
    /// </summary>
    [Range(1, 100)]
    public int BatchSize { get; set; } = 1;

    /// <summary>
    /// Base delay for retry attempts in milliseconds.
    /// </summary>
    [Range(1000, 300000)]
    public int RetryDelay { get; set; } = 30000;

    /// <summary>
    /// Maximum number of retry attempts before giving up.
    /// </summary>
    [Range(1, 10)]
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// Delay between RPC requests in milliseconds (for rate limiting).
    /// </summary>
    [Range(0, 60000)]
    public int RequestDelay { get; set; } = 10000;

    /// <summary>
    /// Whether monitoring is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
