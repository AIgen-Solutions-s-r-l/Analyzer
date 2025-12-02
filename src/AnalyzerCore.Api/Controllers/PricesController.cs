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
/// <remarks>
/// The Prices API provides real-time and historical price data for ERC-20 tokens.
///
/// ## Features
/// - **Spot Price**: Get current token prices against various quote currencies
/// - **USD Price**: Get token prices converted to USD via stablecoin pools
/// - **TWAP**: Calculate Time-Weighted Average Price for manipulation resistance
/// - **Price History**: Query historical price data for charting and analysis
///
/// ## Quote Currencies
/// Supported quote currencies: ETH, USDC, USDT, DAI, USD (virtual)
///
/// ## Rate Limits
/// - Standard tier: 100 requests/minute
/// - Premium tier: 1000 requests/minute
///
/// ## Authentication
/// All endpoints require API key authentication via Bearer token or X-API-Key header.
/// </remarks>
[Authorize(Policy = "RequireReadOnly")]
[Produces("application/json")]
[Tags("Prices")]
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
    /// Gets the current spot price of a token.
    /// </summary>
    /// <remarks>
    /// Returns the current price of a token from the pool with the highest liquidity.
    /// The price is calculated from DEX pool reserves using the constant product formula.
    ///
    /// ### Example Request
    /// ```
    /// GET /api/v1/prices/0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2?quoteCurrency=USDT
    /// ```
    ///
    /// ### Example Response
    /// ```json
    /// {
    ///   "tokenAddress": "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
    ///   "quoteTokenAddress": "0xdac17f958d2ee523a2206206994597c13d831ec7",
    ///   "quoteTokenSymbol": "USDT",
    ///   "price": 1850.50,
    ///   "priceUsd": 1850.50,
    ///   "poolAddress": "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
    ///   "liquidity": 5000000.00,
    ///   "timestamp": "2025-01-15T10:30:00Z"
    /// }
    /// ```
    ///
    /// ### Price Calculation
    /// Price is derived from pool reserves: `price = reserve1 / reserve0`
    /// The pool with the highest liquidity is automatically selected for accuracy.
    /// </remarks>
    /// <param name="tokenAddress">The ERC-20 token contract address (0x-prefixed, 42 characters).</param>
    /// <param name="quoteCurrency">The quote currency. Supported: ETH, USDC, USDT, DAI, USD. Default: ETH.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current token price with metadata.</returns>
    /// <response code="200">Price retrieved successfully.</response>
    /// <response code="400">Invalid token address format.</response>
    /// <response code="404">Token not found or no liquidity pools available.</response>
    /// <response code="401">Unauthorized - missing or invalid API key.</response>
    [HttpGet("{tokenAddress}")]
    [ProducesResponseType(typeof(TokenPriceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
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
    /// <remarks>
    /// Returns the token price converted to USD using stablecoin pool routing.
    /// The conversion uses the best available path through USDC, USDT, or DAI pools.
    ///
    /// ### Example Request
    /// ```
    /// GET /api/v1/prices/0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2/usd
    /// ```
    ///
    /// ### Price Routing
    /// 1. Direct: Token → Stablecoin pool
    /// 2. Multi-hop: Token → ETH → Stablecoin (if no direct pool)
    ///
    /// ### Stablecoin Pricing
    /// USDC, USDT, and DAI are assumed to be $1.00 USD for conversion.
    /// </remarks>
    /// <param name="tokenAddress">The ERC-20 token contract address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token price in USD.</returns>
    /// <response code="200">USD price retrieved successfully.</response>
    /// <response code="400">Invalid token address format.</response>
    /// <response code="404">Token not found or no USD pricing route available.</response>
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
