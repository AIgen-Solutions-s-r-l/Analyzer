namespace AnalyzerCore.Api.Contracts.Prices;

/// <summary>
/// Response containing token price information.
/// </summary>
public sealed record TokenPriceResponse
{
    /// <summary>
    /// The token address.
    /// </summary>
    public string TokenAddress { get; init; } = string.Empty;

    /// <summary>
    /// The quote token address.
    /// </summary>
    public string QuoteTokenAddress { get; init; } = string.Empty;

    /// <summary>
    /// The quote token symbol.
    /// </summary>
    public string QuoteTokenSymbol { get; init; } = string.Empty;

    /// <summary>
    /// The price in quote currency.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// The price in USD.
    /// </summary>
    public decimal PriceUsd { get; init; }

    /// <summary>
    /// The pool address used for the price.
    /// </summary>
    public string PoolAddress { get; init; } = string.Empty;

    /// <summary>
    /// The liquidity in the pool.
    /// </summary>
    public decimal Liquidity { get; init; }

    /// <summary>
    /// Timestamp of the price.
    /// </summary>
    public DateTime Timestamp { get; init; }
}
