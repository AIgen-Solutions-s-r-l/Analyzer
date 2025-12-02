namespace AnalyzerCore.Api.Contracts.Prices;

/// <summary>
/// Response containing TWAP calculation results.
/// </summary>
public sealed record TwapResponse
{
    /// <summary>
    /// The token address.
    /// </summary>
    public string TokenAddress { get; init; } = string.Empty;

    /// <summary>
    /// The quote token symbol.
    /// </summary>
    public string QuoteTokenSymbol { get; init; } = string.Empty;

    /// <summary>
    /// The time-weighted average price.
    /// </summary>
    public decimal TwapPrice { get; init; }

    /// <summary>
    /// The current spot price.
    /// </summary>
    public decimal SpotPrice { get; init; }

    /// <summary>
    /// Deviation between TWAP and spot price (percentage).
    /// </summary>
    public decimal PriceDeviation { get; init; }

    /// <summary>
    /// The period over which TWAP was calculated (in minutes).
    /// </summary>
    public int PeriodMinutes { get; init; }

    /// <summary>
    /// Number of data points used.
    /// </summary>
    public int DataPoints { get; init; }

    /// <summary>
    /// When the calculation was performed.
    /// </summary>
    public DateTime CalculatedAt { get; init; }
}
