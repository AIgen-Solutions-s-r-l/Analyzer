using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Infrastructure.Configuration;

/// <summary>
/// Configuration options for the arbitrage scanner background service.
/// </summary>
public sealed class ArbitrageScannerOptions
{
    public const string SectionName = "ArbitrageScanner";

    /// <summary>
    /// Whether the arbitrage scanner is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval between scans in milliseconds.
    /// </summary>
    [Range(1000, 300000)]
    public int ScanIntervalMs { get; set; } = 30000; // 30 seconds

    /// <summary>
    /// Minimum profit threshold in USD to report an opportunity.
    /// </summary>
    [Range(0, 100000)]
    public decimal MinProfitUsd { get; set; } = 10m;

    /// <summary>
    /// Threshold in USD for large opportunity alerts.
    /// </summary>
    [Range(0, 1000000)]
    public decimal LargeOpportunityThresholdUsd { get; set; } = 100m;

    /// <summary>
    /// Minimum confidence score (0-100) to trigger large opportunity alerts.
    /// </summary>
    [Range(0, 100)]
    public int MinConfidenceScore { get; set; } = 60;

    /// <summary>
    /// Delay in milliseconds before retrying after an error.
    /// </summary>
    [Range(1000, 60000)]
    public int ErrorRetryDelayMs { get; set; } = 5000;

    /// <summary>
    /// Whether to scan for triangular arbitrage opportunities.
    /// </summary>
    public bool EnableTriangularScan { get; set; } = true;

    /// <summary>
    /// Base token address for triangular arbitrage (default: WETH).
    /// </summary>
    public string TriangularBaseToken { get; set; } = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";

    /// <summary>
    /// Maximum number of cached opportunity hashes before cleanup.
    /// </summary>
    [Range(100, 100000)]
    public int MaxCachedOpportunities { get; set; } = 10000;
}
