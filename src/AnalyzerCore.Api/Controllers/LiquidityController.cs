using AnalyzerCore.Api.Contracts.Liquidity;
using AnalyzerCore.Application.Liquidity.Queries.GetPoolMetrics;
using AnalyzerCore.Application.Liquidity.Queries.GetTokenLiquidity;
using AnalyzerCore.Application.Liquidity.Queries.GetTopPools;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzerCore.Api.Controllers;

/// <summary>
/// API endpoints for liquidity analytics.
/// </summary>
/// <remarks>
/// The Liquidity API provides comprehensive analytics for DEX liquidity pools,
/// helping users understand market depth, yield opportunities, and risk factors.
///
/// ## Features
/// - **Pool Metrics**: TVL, volume, fees, and APR for individual pools
/// - **Token Liquidity**: Aggregate liquidity across all pools for a token
/// - **Top Pools**: Discover the highest TVL pools
/// - **Impermanent Loss**: Calculate IL for liquidity provider positions
/// - **Concentration Analysis**: Assess liquidity distribution risks
///
/// ## Key Metrics Explained
/// - **TVL (Total Value Locked)**: Total USD value of assets in the pool
/// - **Volume 24h**: Trading volume over the last 24 hours
/// - **APR**: Annualized returns from trading fees
/// - **Depth Score**: Measure of how much can be traded without significant slippage
/// - **HHI Index**: Herfindahl-Hirschman Index measuring concentration
///
/// ## Use Cases
/// - Evaluate pool health before providing liquidity
/// - Monitor LP position performance
/// - Find high-yield opportunities
/// - Assess token liquidity depth for trading
///
/// ## Rate Limits
/// - Standard tier: 100 requests/minute
/// - Premium tier: 1000 requests/minute
/// </remarks>
[Authorize(Policy = "RequireReadOnly")]
[Produces("application/json")]
[Tags("Liquidity")]
public class LiquidityController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly ILiquidityAnalyticsService _liquidityService;

    public LiquidityController(ISender sender, ILiquidityAnalyticsService liquidityService)
    {
        _sender = sender;
        _liquidityService = liquidityService;
    }

    /// <summary>
    /// Gets liquidity metrics for a pool.
    /// </summary>
    /// <remarks>
    /// Returns comprehensive liquidity metrics for a specific DEX pool including
    /// TVL, trading volume, fees, and calculated APR.
    ///
    /// ### Example Request
    /// ```
    /// GET /api/v1/liquidity/pools/0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852
    /// ```
    ///
    /// ### Example Response
    /// ```json
    /// {
    ///   "poolAddress": "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
    ///   "token0Address": "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
    ///   "token0Symbol": "WETH",
    ///   "token1Address": "0xdac17f958d2ee523a2206206994597c13d831ec7",
    ///   "token1Symbol": "USDT",
    ///   "tvlUsd": 125000000.00,
    ///   "reserve0": 35000.50,
    ///   "reserve1": 65000000.00,
    ///   "volume24hUsd": 45000000.00,
    ///   "fees24hUsd": 135000.00,
    ///   "aprPercent": 39.42,
    ///   "depthScore": 0.92,
    ///   "timestamp": "2025-01-15T10:30:00Z"
    /// }
    /// ```
    ///
    /// ### APR Calculation
    /// APR is calculated from 24h fees annualized: `(fees24h * 365 / tvl) * 100`
    ///
    /// ### Depth Score
    /// Score from 0-1 indicating how much can be traded without 2% slippage.
    /// Higher is better for large trades.
    /// </remarks>
    /// <param name="poolAddress">The liquidity pool contract address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pool liquidity metrics including TVL, volume, fees, and APR.</returns>
    /// <response code="200">Metrics retrieved successfully.</response>
    /// <response code="400">Invalid pool address format.</response>
    /// <response code="401">Unauthorized - missing or invalid API key.</response>
    /// <response code="404">Pool not found or not monitored.</response>
    [HttpGet("pools/{poolAddress}")]
    [ProducesResponseType(typeof(LiquidityMetricsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPoolMetrics(
        [FromRoute] string poolAddress,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPoolMetricsQuery { PoolAddress = poolAddress };
        var result = await _sender.Send(query, cancellationToken);

        return result.Match(
            onSuccess: metrics => Ok(MapMetricsToResponse(metrics)),
            onFailure: _ => ToActionResult(result));
    }

    /// <summary>
    /// Gets liquidity summary for a token.
    /// </summary>
    /// <remarks>
    /// Aggregates liquidity data across all pools containing the specified token,
    /// providing a comprehensive view of the token's market depth and distribution.
    ///
    /// ### Example Request
    /// ```
    /// GET /api/v1/liquidity/tokens/0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2
    /// ```
    ///
    /// ### Example Response
    /// ```json
    /// {
    ///   "tokenAddress": "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
    ///   "tokenSymbol": "WETH",
    ///   "totalLiquidityUsd": 850000000.00,
    ///   "poolCount": 245,
    ///   "topPools": [
    ///     {
    ///       "poolAddress": "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
    ///       "pairedTokenSymbol": "USDT",
    ///       "liquidityUsd": 125000000.00,
    ///       "sharePercent": 14.71
    ///     }
    ///   ],
    ///   "averageLiquidityPerPool": 3469387.76,
    ///   "totalVolume24hUsd": 320000000.00
    /// }
    /// ```
    ///
    /// ### Use Cases
    /// - Assess overall token liquidity before large trades
    /// - Find the best pools for trading a specific token
    /// - Evaluate token market health and adoption
    /// </remarks>
    /// <param name="tokenAddress">The ERC-20 token contract address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated token liquidity summary with top pools.</returns>
    /// <response code="200">Summary retrieved successfully.</response>
    /// <response code="400">Invalid token address format.</response>
    /// <response code="401">Unauthorized - missing or invalid API key.</response>
    /// <response code="404">Token not found in any monitored pool.</response>
    [HttpGet("tokens/{tokenAddress}")]
    [ProducesResponseType(typeof(TokenLiquiditySummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTokenLiquidity(
        [FromRoute] string tokenAddress,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTokenLiquidityQuery { TokenAddress = tokenAddress };
        var result = await _sender.Send(query, cancellationToken);

        return result.Match(
            onSuccess: summary => Ok(MapSummaryToResponse(summary)),
            onFailure: _ => ToActionResult(result));
    }

    /// <summary>
    /// Gets top pools by TVL.
    /// </summary>
    /// <remarks>
    /// Returns the highest TVL (Total Value Locked) liquidity pools across all
    /// monitored DEXs, sorted by descending TVL.
    ///
    /// ### Example Request
    /// ```
    /// GET /api/v1/liquidity/top-pools?limit=5
    /// ```
    ///
    /// ### Use Cases
    /// - Discover high-liquidity pools for trading
    /// - Find yield farming opportunities
    /// - Monitor market trends and capital flows
    ///
    /// ### Pagination
    /// Use the `limit` parameter to control results (max 100).
    /// For more pools, consider using the token-specific endpoint.
    /// </remarks>
    /// <param name="limit">Maximum number of pools to return (default: 10, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Top pools sorted by TVL with full metrics.</returns>
    /// <response code="200">Top pools retrieved successfully.</response>
    /// <response code="401">Unauthorized - missing or invalid API key.</response>
    [HttpGet("top-pools")]
    [ProducesResponseType(typeof(IEnumerable<LiquidityMetricsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopPools(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTopPoolsQuery { Limit = limit };
        var result = await _sender.Send(query, cancellationToken);

        return result.Match(
            onSuccess: pools => Ok(pools.Select(MapMetricsToResponse)),
            onFailure: _ => ToActionResult(result));
    }

    /// <summary>
    /// Calculates impermanent loss for a position.
    /// </summary>
    /// <remarks>
    /// Calculates the impermanent loss (IL) for a liquidity provider position,
    /// comparing LP returns against simply holding the tokens (HODL).
    ///
    /// ### Example Request
    /// ```json
    /// POST /api/v1/liquidity/impermanent-loss
    /// {
    ///   "poolAddress": "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
    ///   "entryPriceRatio": 1850.00,
    ///   "initialInvestmentUsd": 10000.00
    /// }
    /// ```
    ///
    /// ### Example Response
    /// ```json
    /// {
    ///   "poolAddress": "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
    ///   "initialPriceRatio": 1850.00,
    ///   "currentPriceRatio": 2200.00,
    ///   "priceChangePercent": 18.92,
    ///   "impermanentLossPercent": 0.83,
    ///   "hodlValueUsd": 11892.00,
    ///   "lpValueUsd": 11793.27,
    ///   "differenceUsd": -98.73,
    ///   "calculatedAt": "2025-01-15T10:30:00Z"
    /// }
    /// ```
    ///
    /// ### Understanding Impermanent Loss
    /// IL occurs when the price ratio between tokens changes from entry.
    /// - 1.25x price change: ~0.6% IL
    /// - 1.50x price change: ~2.0% IL
    /// - 2.00x price change: ~5.7% IL
    /// - 5.00x price change: ~25.5% IL
    ///
    /// ### Important Notes
    /// - IL becomes "permanent" only when you withdraw
    /// - Trading fees may offset IL over time
    /// - IL is the same whether price goes up or down by same ratio
    /// </remarks>
    /// <param name="request">Impermanent loss calculation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed impermanent loss calculation comparing LP vs HODL.</returns>
    /// <response code="200">Calculation completed successfully.</response>
    /// <response code="400">Invalid parameters or pool address.</response>
    /// <response code="401">Unauthorized - missing or invalid API key.</response>
    /// <response code="404">Pool not found.</response>
    [HttpPost("impermanent-loss")]
    [ProducesResponseType(typeof(ImpermanentLossResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateImpermanentLoss(
        [FromBody] ImpermanentLossRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _liquidityService.CalculateImpermanentLossAsync(
            request.PoolAddress,
            request.EntryPriceRatio,
            request.InitialInvestmentUsd,
            cancellationToken);

        return result.Match(
            onSuccess: il => Ok(new ImpermanentLossResponse
            {
                PoolAddress = il.PoolAddress,
                InitialPriceRatio = il.InitialPriceRatio,
                CurrentPriceRatio = il.CurrentPriceRatio,
                PriceChangePercent = il.PriceChangePercent,
                ImpermanentLossPercent = il.ImpermanentLossPercent,
                HodlValueUsd = il.HodlValueUsd,
                LpValueUsd = il.LpValueUsd,
                DifferenceUsd = il.DifferenceUsd,
                CalculatedAt = il.CalculatedAt
            }),
            onFailure: _ => ToActionResult(result));
    }

    /// <summary>
    /// Gets liquidity concentration analysis for a token.
    /// </summary>
    /// <remarks>
    /// Analyzes the distribution of liquidity across pools for a token,
    /// helping assess concentration risk and market health.
    ///
    /// ### Example Request
    /// ```
    /// GET /api/v1/liquidity/concentration/0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2
    /// ```
    ///
    /// ### Example Response
    /// ```json
    /// {
    ///   "tokenAddress": "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
    ///   "totalLiquidityUsd": 850000000.00,
    ///   "topPoolConcentration": 0.147,
    ///   "top3PoolsConcentration": 0.385,
    ///   "top5PoolsConcentration": 0.512,
    ///   "hhiIndex": 0.089,
    ///   "concentrationLevel": "Low",
    ///   "poolCount": 245
    /// }
    /// ```
    ///
    /// ### HHI Index Interpretation
    /// The Herfindahl-Hirschman Index (HHI) measures market concentration:
    /// - Below 0.01: Highly competitive (Low concentration)
    /// - 0.01 - 0.15: Unconcentrated (Low concentration)
    /// - 0.15 - 0.25: Moderately concentrated (Medium concentration)
    /// - Above 0.25: Highly concentrated (High concentration)
    ///
    /// ### Risk Implications
    /// - **Low concentration**: Healthy market, multiple venues for trading
    /// - **High concentration**: Single pool dominance, higher manipulation risk
    /// </remarks>
    /// <param name="tokenAddress">The ERC-20 token contract address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Liquidity concentration metrics and risk assessment.</returns>
    /// <response code="200">Concentration data retrieved successfully.</response>
    /// <response code="400">Invalid token address format.</response>
    /// <response code="401">Unauthorized - missing or invalid API key.</response>
    /// <response code="404">Token not found in any monitored pool.</response>
    [HttpGet("concentration/{tokenAddress}")]
    [ProducesResponseType(typeof(LiquidityConcentrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLiquidityConcentration(
        [FromRoute] string tokenAddress,
        CancellationToken cancellationToken = default)
    {
        var result = await _liquidityService.GetLiquidityConcentrationAsync(
            tokenAddress, cancellationToken);

        return result.Match(
            onSuccess: concentration => Ok(new LiquidityConcentrationResponse
            {
                TokenAddress = concentration.TokenAddress,
                TotalLiquidityUsd = concentration.TotalLiquidityUsd,
                TopPoolConcentration = concentration.TopPoolConcentration,
                Top3PoolsConcentration = concentration.Top3PoolsConcentration,
                Top5PoolsConcentration = concentration.Top5PoolsConcentration,
                HhiIndex = concentration.HhiIndex,
                ConcentrationLevel = concentration.ConcentrationLevel,
                PoolCount = concentration.PoolCount
            }),
            onFailure: _ => ToActionResult(result));
    }

    private static LiquidityMetricsResponse MapMetricsToResponse(LiquidityMetrics metrics) => new()
    {
        PoolAddress = metrics.PoolAddress,
        Token0Address = metrics.Token0Address,
        Token0Symbol = metrics.Token0Symbol,
        Token1Address = metrics.Token1Address,
        Token1Symbol = metrics.Token1Symbol,
        TvlUsd = metrics.TvlUsd,
        Reserve0 = metrics.Reserve0,
        Reserve1 = metrics.Reserve1,
        Reserve0Usd = metrics.Reserve0Usd,
        Reserve1Usd = metrics.Reserve1Usd,
        Volume24hUsd = metrics.Volume24hUsd,
        Fees24hUsd = metrics.Fees24hUsd,
        AprPercent = metrics.AprPercent,
        DepthScore = metrics.DepthScore,
        Timestamp = metrics.Timestamp
    };

    private static TokenLiquiditySummaryResponse MapSummaryToResponse(TokenLiquiditySummary summary) => new()
    {
        TokenAddress = summary.TokenAddress,
        TokenSymbol = summary.TokenSymbol,
        TotalLiquidityUsd = summary.TotalLiquidityUsd,
        PoolCount = summary.PoolCount,
        TopPools = summary.TopPools.Select(p => new PoolLiquiditySummaryResponse
        {
            PoolAddress = p.PoolAddress,
            PairedTokenAddress = p.PairedTokenAddress,
            PairedTokenSymbol = p.PairedTokenSymbol,
            LiquidityUsd = p.LiquidityUsd,
            SharePercent = p.SharePercent
        }).ToList(),
        AverageLiquidityPerPool = summary.AverageLiquidityPerPool,
        TotalVolume24hUsd = summary.TotalVolume24hUsd,
        Timestamp = summary.Timestamp
    };
}
