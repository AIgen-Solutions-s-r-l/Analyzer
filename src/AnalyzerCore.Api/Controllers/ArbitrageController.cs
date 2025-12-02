using AnalyzerCore.Api.Contracts.Arbitrage;
using AnalyzerCore.Application.Arbitrage.Queries.GetTokenArbitrage;
using AnalyzerCore.Application.Arbitrage.Queries.ScanArbitrage;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzerCore.Api.Controllers;

/// <summary>
/// API endpoints for arbitrage detection and analysis.
/// </summary>
/// <remarks>
/// The Arbitrage API provides tools for detecting and analyzing price discrepancies
/// across decentralized exchanges (DEXs) that can be exploited for profit.
///
/// ## Features
/// - **Market Scan**: Scan all monitored pools for arbitrage opportunities
/// - **Token-Specific**: Find opportunities for a specific token
/// - **Triangular Arbitrage**: Detect multi-hop arbitrage paths (A→B→C→A)
/// - **Optimal Calculation**: Calculate optimal trade sizes for maximum profit
///
/// ## How It Works
/// 1. The system monitors price feeds from multiple DEX pools
/// 2. When price discrepancies exceed thresholds, opportunities are flagged
/// 3. Gas costs are estimated and factored into profitability calculations
/// 4. Confidence scores indicate reliability of the opportunity
///
/// ## Risk Considerations
/// - Opportunities may disappear before execution (MEV competition)
/// - Gas price spikes can eliminate profits
/// - Slippage may reduce actual returns
/// - Smart contract risks on DEX interactions
///
/// ## Rate Limits
/// - Standard tier: 60 requests/minute
/// - Premium tier: 600 requests/minute
/// </remarks>
[Authorize(Policy = "RequireReadOnly")]
[Produces("application/json")]
[Tags("Arbitrage")]
public class ArbitrageController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly IArbitrageService _arbitrageService;

    public ArbitrageController(ISender sender, IArbitrageService arbitrageService)
    {
        _sender = sender;
        _arbitrageService = arbitrageService;
    }

    /// <summary>
    /// Scans all pools for arbitrage opportunities.
    /// </summary>
    /// <remarks>
    /// Performs a comprehensive scan across all monitored liquidity pools to identify
    /// price discrepancies that could yield profitable arbitrage trades.
    ///
    /// ### Example Request
    /// ```
    /// GET /api/v1/arbitrage/scan?minProfitUsd=50
    /// ```
    ///
    /// ### Example Response
    /// ```json
    /// [
    ///   {
    ///     "id": "arb_001",
    ///     "tokenAddress": "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
    ///     "tokenSymbol": "WETH",
    ///     "buyPrice": 1845.50,
    ///     "sellPrice": 1852.30,
    ///     "spreadPercent": 0.37,
    ///     "expectedProfitUsd": 68.00,
    ///     "estimatedGasCostUsd": 12.50,
    ///     "netProfitUsd": 55.50,
    ///     "isProfitable": true,
    ///     "confidenceScore": 0.85,
    ///     "detectedAt": "2025-01-15T10:30:00Z"
    ///   }
    /// ]
    /// ```
    ///
    /// ### Filtering
    /// Use `minProfitUsd` to filter out low-value opportunities. Higher thresholds
    /// return fewer but more significant opportunities.
    ///
    /// ### Confidence Scores
    /// - 0.9+: High confidence, stable price differential
    /// - 0.7-0.9: Medium confidence, volatile market
    /// - Below 0.7: Low confidence, may disappear quickly
    /// </remarks>
    /// <param name="minProfitUsd">Minimum net profit threshold in USD after gas costs (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of arbitrage opportunities sorted by profitability.</returns>
    /// <response code="200">Opportunities found successfully.</response>
    /// <response code="401">Unauthorized - missing or invalid API key.</response>
    [HttpGet("scan")]
    [ProducesResponseType(typeof(IEnumerable<ArbitrageOpportunityResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Scan(
        [FromQuery] decimal minProfitUsd = 10m,
        CancellationToken cancellationToken = default)
    {
        var query = new ScanArbitrageQuery
        {
            MinProfitUsd = minProfitUsd
        };

        var result = await _sender.Send(query, cancellationToken);

        return result.Match(
            onSuccess: opportunities => Ok(opportunities.Select(MapToResponse)),
            onFailure: _ => ToActionResult(result));
    }

    /// <summary>
    /// Finds arbitrage opportunities for a specific token.
    /// </summary>
    /// <remarks>
    /// Searches for arbitrage opportunities involving a specific token across all
    /// monitored DEX pools. Useful for tracking opportunities for tokens you're interested in.
    ///
    /// ### Example Request
    /// ```
    /// GET /api/v1/arbitrage/token/0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2
    /// ```
    ///
    /// ### Use Cases
    /// - Monitor specific assets for trading opportunities
    /// - Track price efficiency across markets for a token
    /// - Build automated trading strategies around specific tokens
    ///
    /// ### Token Address Format
    /// Must be a valid ERC-20 contract address:
    /// - 42 characters including '0x' prefix
    /// - Case-insensitive (checksummed addresses recommended)
    /// </remarks>
    /// <param name="tokenAddress">The ERC-20 token contract address (0x-prefixed, 42 characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Arbitrage opportunities for the specified token.</returns>
    /// <response code="200">Opportunities retrieved successfully.</response>
    /// <response code="400">Invalid token address format.</response>
    /// <response code="401">Unauthorized - missing or invalid API key.</response>
    [HttpGet("token/{tokenAddress}")]
    [ProducesResponseType(typeof(IEnumerable<ArbitrageOpportunityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByToken(
        [FromRoute] string tokenAddress,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTokenArbitrageQuery
        {
            TokenAddress = tokenAddress
        };

        var result = await _sender.Send(query, cancellationToken);

        return result.Match(
            onSuccess: opportunities => Ok(opportunities.Select(MapToResponse)),
            onFailure: _ => ToActionResult(result));
    }

    /// <summary>
    /// Finds triangular arbitrage opportunities.
    /// </summary>
    /// <remarks>
    /// Detects triangular arbitrage paths where profit is made by trading through
    /// three or more pools in a cycle, returning to the starting token.
    ///
    /// ### Example Request
    /// ```
    /// GET /api/v1/arbitrage/triangular?baseToken=0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2
    /// ```
    ///
    /// ### How Triangular Arbitrage Works
    /// ```
    /// WETH → USDC → DAI → WETH
    ///   Pool 1    Pool 2    Pool 3
    /// ```
    ///
    /// Starting with 1 WETH:
    /// 1. Swap WETH → USDC at Pool 1 (get 1850 USDC)
    /// 2. Swap USDC → DAI at Pool 2 (get 1855 DAI)
    /// 3. Swap DAI → WETH at Pool 3 (get 1.003 WETH)
    /// 4. Profit: 0.003 WETH
    ///
    /// ### Complexity
    /// Triangular arbitrage requires more gas (3 swaps) but often finds
    /// opportunities missed by simple two-pool arbitrage.
    ///
    /// ### Default Base Token
    /// WETH (0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2) is the default base
    /// as it has the highest liquidity and most trading pairs.
    /// </remarks>
    /// <param name="baseToken">Base token address to start/end the cycle (default: WETH).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Triangular arbitrage opportunities with full path details.</returns>
    /// <response code="200">Opportunities retrieved successfully.</response>
    /// <response code="401">Unauthorized - missing or invalid API key.</response>
    [HttpGet("triangular")]
    [ProducesResponseType(typeof(IEnumerable<ArbitrageOpportunityResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTriangular(
        [FromQuery] string baseToken = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
        CancellationToken cancellationToken = default)
    {
        var result = await _arbitrageService.FindTriangularOpportunitiesAsync(
            baseToken, cancellationToken);

        return result.Match(
            onSuccess: opportunities => Ok(opportunities.Select(MapToResponse)),
            onFailure: _ => ToActionResult(result));
    }

    /// <summary>
    /// Calculates optimal arbitrage amount between two pools.
    /// </summary>
    /// <remarks>
    /// Calculates the mathematically optimal input amount for a two-pool arbitrage
    /// trade to maximize profit while accounting for slippage and pool depth.
    ///
    /// ### Example Request
    /// ```
    /// GET /api/v1/arbitrage/calculate?buyPool=0x1234...&amp;sellPool=0x5678...&amp;tokenAddress=0xabcd...
    /// ```
    ///
    /// ### Example Response
    /// ```json
    /// {
    ///   "optimalInputAmount": 5.25,
    ///   "expectedProfit": 0.0125
    /// }
    /// ```
    ///
    /// ### Optimization Algorithm
    /// Uses the constant product formula to find the sweet spot:
    /// - Too small: Profit doesn't cover gas costs
    /// - Too large: Slippage erodes profits
    /// - Optimal: Maximum net profit after gas
    ///
    /// ### Important Considerations
    /// - Result is theoretical; actual execution may vary
    /// - Does not account for MEV/frontrunning competition
    /// - Pool reserves may change between calculation and execution
    /// </remarks>
    /// <param name="buyPool">Pool address where the token will be purchased (lower price).</param>
    /// <param name="sellPool">Pool address where the token will be sold (higher price).</param>
    /// <param name="tokenAddress">The ERC-20 token being arbitraged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Optimal input amount and expected profit in token units.</returns>
    /// <response code="200">Calculation successful.</response>
    /// <response code="400">Invalid pool or token addresses.</response>
    /// <response code="401">Unauthorized - missing or invalid API key.</response>
    /// <response code="404">Pool or token not found.</response>
    [HttpGet("calculate")]
    [ProducesResponseType(typeof(OptimalArbitrageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateOptimal(
        [FromQuery] string buyPool,
        [FromQuery] string sellPool,
        [FromQuery] string tokenAddress,
        CancellationToken cancellationToken = default)
    {
        var result = await _arbitrageService.CalculateOptimalAmountAsync(
            buyPool, sellPool, tokenAddress, cancellationToken);

        return result.Match(
            onSuccess: calc => Ok(new OptimalArbitrageResponse
            {
                OptimalInputAmount = calc.OptimalInput,
                ExpectedProfit = calc.ExpectedProfit
            }),
            onFailure: _ => ToActionResult(result));
    }

    private static ArbitrageOpportunityResponse MapToResponse(ArbitrageOpportunity opportunity) => new()
    {
        Id = opportunity.Id,
        TokenAddress = opportunity.TokenAddress,
        TokenSymbol = opportunity.TokenSymbol,
        Path = opportunity.Path.Select(MapLegToResponse).ToList(),
        BuyPrice = opportunity.BuyPrice,
        SellPrice = opportunity.SellPrice,
        SpreadPercent = opportunity.SpreadPercent,
        ExpectedProfitUsd = opportunity.ExpectedProfitUsd,
        OptimalInputAmount = opportunity.OptimalInputAmount,
        EstimatedGasCostUsd = opportunity.EstimatedGasCostUsd,
        NetProfitUsd = opportunity.NetProfitUsd,
        RoiPercent = opportunity.RoiPercent,
        IsProfitable = opportunity.IsProfitable,
        ConfidenceScore = opportunity.ConfidenceScore,
        DetectedAt = opportunity.DetectedAt
    };

    private static ArbitrageLegResponse MapLegToResponse(ArbitrageLeg leg) => new()
    {
        PoolAddress = leg.PoolAddress,
        DexName = leg.DexName,
        TokenIn = leg.TokenIn,
        TokenOut = leg.TokenOut,
        Rate = leg.Rate,
        Liquidity = leg.Liquidity
    };
}

/// <summary>
/// Response for optimal arbitrage calculation.
/// </summary>
public sealed record OptimalArbitrageResponse
{
    /// <summary>
    /// Optimal input amount.
    /// </summary>
    public decimal OptimalInputAmount { get; init; }

    /// <summary>
    /// Expected profit in token units.
    /// </summary>
    public decimal ExpectedProfit { get; init; }
}
