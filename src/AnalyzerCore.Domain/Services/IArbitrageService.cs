using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Domain.Services;

/// <summary>
/// Service interface for arbitrage detection and analysis.
/// </summary>
public interface IArbitrageService
{
    /// <summary>
    /// Scans all pools for arbitrage opportunities.
    /// </summary>
    /// <param name="minProfitUsd">Minimum profit threshold in USD.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of detected arbitrage opportunities.</returns>
    Task<Result<IReadOnlyList<ArbitrageOpportunity>>> ScanForOpportunitiesAsync(
        decimal minProfitUsd = 10m,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds arbitrage opportunities for a specific token.
    /// </summary>
    /// <param name="tokenAddress">The token to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of arbitrage opportunities for the token.</returns>
    Task<Result<IReadOnlyList<ArbitrageOpportunity>>> FindOpportunitiesForTokenAsync(
        string tokenAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds triangular arbitrage opportunities.
    /// </summary>
    /// <param name="baseToken">The base token (usually WETH or stablecoin).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of triangular arbitrage opportunities.</returns>
    Task<Result<IReadOnlyList<ArbitrageOpportunity>>> FindTriangularOpportunitiesAsync(
        string baseToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the optimal input amount for maximum profit.
    /// </summary>
    /// <param name="buyPool">Pool address to buy from.</param>
    /// <param name="sellPool">Pool address to sell to.</param>
    /// <param name="tokenAddress">Token being arbitraged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Optimal input amount and expected profit.</returns>
    Task<Result<(decimal OptimalInput, decimal ExpectedProfit)>> CalculateOptimalAmountAsync(
        string buyPool,
        string sellPool,
        string tokenAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates gas cost for an arbitrage execution.
    /// </summary>
    /// <param name="opportunity">The arbitrage opportunity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Estimated gas cost in USD.</returns>
    Task<Result<decimal>> EstimateGasCostAsync(
        ArbitrageOpportunity opportunity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical arbitrage opportunities.
    /// </summary>
    /// <param name="from">Start of time range.</param>
    /// <param name="to">End of time range.</param>
    /// <param name="minProfitUsd">Minimum profit filter.</param>
    /// <param name="limit">Maximum records to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Historical arbitrage data.</returns>
    Task<Result<IReadOnlyList<ArbitrageOpportunity>>> GetHistoricalOpportunitiesAsync(
        DateTime? from = null,
        DateTime? to = null,
        decimal? minProfitUsd = null,
        int limit = 100,
        CancellationToken cancellationToken = default);
}
