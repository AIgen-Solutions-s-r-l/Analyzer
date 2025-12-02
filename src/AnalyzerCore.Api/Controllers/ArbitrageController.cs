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
[Authorize(Policy = "RequireReadOnly")]
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
    /// <param name="minProfitUsd">Minimum profit threshold in USD (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of arbitrage opportunities.</returns>
    /// <response code="200">Opportunities found successfully.</response>
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
    /// <param name="tokenAddress">The token address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Arbitrage opportunities for the token.</returns>
    /// <response code="200">Opportunities retrieved successfully.</response>
    /// <response code="400">Invalid token address.</response>
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
    /// <param name="baseToken">Base token address (default: WETH).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Triangular arbitrage opportunities.</returns>
    /// <response code="200">Opportunities retrieved successfully.</response>
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
    /// <param name="buyPool">Pool address to buy from.</param>
    /// <param name="sellPool">Pool address to sell to.</param>
    /// <param name="tokenAddress">Token being arbitraged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Optimal input and expected profit.</returns>
    /// <response code="200">Calculation successful.</response>
    /// <response code="400">Invalid parameters.</response>
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
