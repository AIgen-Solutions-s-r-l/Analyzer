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
[Authorize(Policy = "RequireReadOnly")]
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
    /// <param name="poolAddress">The pool address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pool liquidity metrics.</returns>
    /// <response code="200">Metrics retrieved successfully.</response>
    /// <response code="404">Pool not found.</response>
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
    /// <param name="tokenAddress">The token address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token liquidity summary.</returns>
    /// <response code="200">Summary retrieved successfully.</response>
    /// <response code="404">Token not found.</response>
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
    /// <param name="limit">Maximum number of pools (default: 10, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Top pools with metrics.</returns>
    /// <response code="200">Top pools retrieved successfully.</response>
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
    /// <param name="request">Impermanent loss calculation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Impermanent loss calculation.</returns>
    /// <response code="200">Calculation completed successfully.</response>
    /// <response code="400">Invalid parameters.</response>
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
    /// <param name="tokenAddress">The token address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Liquidity concentration data.</returns>
    /// <response code="200">Concentration data retrieved successfully.</response>
    /// <response code="404">Token not found.</response>
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
