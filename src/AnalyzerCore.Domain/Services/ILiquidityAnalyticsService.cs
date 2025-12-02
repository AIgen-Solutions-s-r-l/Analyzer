using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Domain.Services;

/// <summary>
/// Service interface for liquidity analytics and metrics.
/// </summary>
public interface ILiquidityAnalyticsService
{
    /// <summary>
    /// Gets liquidity metrics for a specific pool.
    /// </summary>
    /// <param name="poolAddress">The pool address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pool liquidity metrics.</returns>
    Task<Result<LiquidityMetrics>> GetPoolMetricsAsync(
        string poolAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets liquidity summary for a token across all pools.
    /// </summary>
    /// <param name="tokenAddress">The token address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token liquidity summary.</returns>
    Task<Result<TokenLiquiditySummary>> GetTokenLiquiditySummaryAsync(
        string tokenAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top pools by TVL.
    /// </summary>
    /// <param name="limit">Maximum number of pools to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of top pools with metrics.</returns>
    Task<Result<IReadOnlyList<LiquidityMetrics>>> GetTopPoolsByTvlAsync(
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates impermanent loss for a position.
    /// </summary>
    /// <param name="poolAddress">The pool address.</param>
    /// <param name="entryPriceRatio">Price ratio at entry (token1/token0).</param>
    /// <param name="initialInvestmentUsd">Initial investment in USD.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Impermanent loss calculation.</returns>
    Task<Result<ImpermanentLossResult>> CalculateImpermanentLossAsync(
        string poolAddress,
        decimal entryPriceRatio,
        decimal initialInvestmentUsd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical TVL for a pool.
    /// </summary>
    /// <param name="poolAddress">The pool address.</param>
    /// <param name="from">Start of time range.</param>
    /// <param name="to">End of time range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Historical TVL data points.</returns>
    Task<Result<IReadOnlyList<TvlDataPoint>>> GetHistoricalTvlAsync(
        string poolAddress,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets liquidity concentration analysis.
    /// </summary>
    /// <param name="tokenAddress">The token to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Liquidity concentration data.</returns>
    Task<Result<LiquidityConcentration>> GetLiquidityConcentrationAsync(
        string tokenAddress,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// TVL data point for historical charts.
/// </summary>
public sealed record TvlDataPoint
{
    public decimal TvlUsd { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Liquidity concentration analysis.
/// </summary>
public sealed record LiquidityConcentration
{
    /// <summary>
    /// Token address.
    /// </summary>
    public string TokenAddress { get; init; } = string.Empty;

    /// <summary>
    /// Total liquidity in USD.
    /// </summary>
    public decimal TotalLiquidityUsd { get; init; }

    /// <summary>
    /// Percentage held by top pool.
    /// </summary>
    public decimal TopPoolConcentration { get; init; }

    /// <summary>
    /// Percentage held by top 3 pools.
    /// </summary>
    public decimal Top3PoolsConcentration { get; init; }

    /// <summary>
    /// Percentage held by top 5 pools.
    /// </summary>
    public decimal Top5PoolsConcentration { get; init; }

    /// <summary>
    /// Herfindahl-Hirschman Index (0-10000).
    /// Lower is more distributed.
    /// </summary>
    public decimal HhiIndex { get; init; }

    /// <summary>
    /// Concentration level description.
    /// </summary>
    public string ConcentrationLevel { get; init; } = string.Empty;

    /// <summary>
    /// Number of pools.
    /// </summary>
    public int PoolCount { get; init; }
}
