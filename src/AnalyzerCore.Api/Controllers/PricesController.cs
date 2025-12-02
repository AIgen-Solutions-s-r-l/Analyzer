using AnalyzerCore.Api.Contracts.Prices;
using AnalyzerCore.Application.Prices.Queries.GetPriceHistory;
using AnalyzerCore.Application.Prices.Queries.GetTokenPrice;
using AnalyzerCore.Application.Prices.Queries.GetTwap;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzerCore.Api.Controllers;

/// <summary>
/// API endpoints for token price discovery and oracle functionality.
/// </summary>
[Authorize(Policy = "RequireReadOnly")]
public class PricesController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly IPriceService _priceService;

    public PricesController(ISender sender, IPriceService priceService)
    {
        _sender = sender;
        _priceService = priceService;
    }

    /// <summary>
    /// Gets the current price of a token.
    /// </summary>
    /// <param name="tokenAddress">The token address.</param>
    /// <param name="quoteCurrency">The quote currency (ETH, USDC, USDT, DAI, USD).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current token price.</returns>
    /// <response code="200">Price retrieved successfully.</response>
    /// <response code="400">Invalid token address.</response>
    /// <response code="404">Token not found or no liquidity.</response>
    [HttpGet("{tokenAddress}")]
    [ProducesResponseType(typeof(TokenPriceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrice(
        [FromRoute] string tokenAddress,
        [FromQuery] string quoteCurrency = "ETH",
        CancellationToken cancellationToken = default)
    {
        var query = new GetTokenPriceQuery
        {
            TokenAddress = tokenAddress,
            QuoteCurrency = quoteCurrency
        };

        var result = await _sender.Send(query, cancellationToken);

        return result.Match(
            onSuccess: price => Ok(MapToResponse(price)),
            onFailure: _ => ToActionResult(result));
    }

    /// <summary>
    /// Gets the USD price of a token.
    /// </summary>
    /// <param name="tokenAddress">The token address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token price in USD.</returns>
    /// <response code="200">USD price retrieved successfully.</response>
    /// <response code="400">Invalid token address.</response>
    /// <response code="404">Token not found or no liquidity.</response>
    [HttpGet("{tokenAddress}/usd")]
    [ProducesResponseType(typeof(TokenPriceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsdPrice(
        [FromRoute] string tokenAddress,
        CancellationToken cancellationToken = default)
    {
        var result = await _priceService.GetTokenPriceUsdAsync(tokenAddress, cancellationToken);

        return result.Match(
            onSuccess: price => Ok(MapToResponse(price)),
            onFailure: _ => ToActionResult(result));
    }

    /// <summary>
    /// Calculates TWAP (Time-Weighted Average Price) for a token.
    /// </summary>
    /// <param name="tokenAddress">The token address.</param>
    /// <param name="quoteCurrency">The quote currency.</param>
    /// <param name="periodMinutes">The period in minutes (default: 60).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>TWAP calculation result.</returns>
    /// <response code="200">TWAP calculated successfully.</response>
    /// <response code="400">Invalid parameters or insufficient data.</response>
    [HttpGet("{tokenAddress}/twap")]
    [ProducesResponseType(typeof(TwapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTwap(
        [FromRoute] string tokenAddress,
        [FromQuery] string quoteCurrency = "ETH",
        [FromQuery] int periodMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTwapQuery
        {
            TokenAddress = tokenAddress,
            QuoteCurrency = quoteCurrency,
            PeriodMinutes = periodMinutes
        };

        var result = await _sender.Send(query, cancellationToken);

        return result.Match(
            onSuccess: twap => Ok(MapToResponse(twap)),
            onFailure: _ => ToActionResult(result));
    }

    /// <summary>
    /// Gets historical prices for a token.
    /// </summary>
    /// <param name="tokenAddress">The token address.</param>
    /// <param name="quoteCurrency">The quote currency.</param>
    /// <param name="from">Start of time range (optional).</param>
    /// <param name="to">End of time range (optional).</param>
    /// <param name="limit">Maximum records to return (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Historical price data.</returns>
    /// <response code="200">Price history retrieved successfully.</response>
    /// <response code="400">Invalid parameters.</response>
    [HttpGet("{tokenAddress}/history")]
    [ProducesResponseType(typeof(IEnumerable<TokenPriceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPriceHistory(
        [FromRoute] string tokenAddress,
        [FromQuery] string quoteCurrency = "ETH",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPriceHistoryQuery
        {
            TokenAddress = tokenAddress,
            QuoteCurrency = quoteCurrency,
            From = from,
            To = to,
            Limit = Math.Min(limit, 1000)
        };

        var result = await _sender.Send(query, cancellationToken);

        return result.Match(
            onSuccess: prices => Ok(prices.Select(MapToResponse)),
            onFailure: _ => ToActionResult(result));
    }

    /// <summary>
    /// Gets supported quote currencies.
    /// </summary>
    /// <returns>List of supported quote currencies.</returns>
    /// <response code="200">Quote currencies retrieved successfully.</response>
    [HttpGet("quote-currencies")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public IActionResult GetSupportedQuoteCurrencies()
    {
        return Ok(_priceService.GetSupportedQuoteCurrencies());
    }

    private static TokenPriceResponse MapToResponse(TokenPrice price) => new()
    {
        TokenAddress = price.TokenAddress,
        QuoteTokenAddress = price.QuoteTokenAddress,
        QuoteTokenSymbol = price.QuoteTokenSymbol,
        Price = price.Price,
        PriceUsd = price.PriceUsd,
        PoolAddress = price.PoolAddress,
        Liquidity = price.Liquidity,
        Timestamp = price.Timestamp
    };

    private static TwapResponse MapToResponse(TwapResult twap) => new()
    {
        TokenAddress = twap.TokenAddress,
        QuoteTokenSymbol = twap.QuoteTokenSymbol,
        TwapPrice = twap.TwapPrice,
        SpotPrice = twap.SpotPrice,
        PriceDeviation = twap.PriceDeviation,
        PeriodMinutes = (int)twap.Period.TotalMinutes,
        DataPoints = twap.DataPoints,
        CalculatedAt = twap.CalculatedAt
    };
}
