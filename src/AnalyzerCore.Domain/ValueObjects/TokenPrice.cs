namespace AnalyzerCore.Domain.ValueObjects;

/// <summary>
/// Value object representing a token price at a specific point in time.
/// </summary>
public sealed record TokenPrice
{
    public string TokenAddress { get; init; } = string.Empty;
    public string QuoteTokenAddress { get; init; } = string.Empty;
    public string QuoteTokenSymbol { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal PriceUsd { get; init; }
    public DateTime Timestamp { get; init; }
    public string PoolAddress { get; init; } = string.Empty;
    public decimal Liquidity { get; init; }

    /// <summary>
    /// Creates a new TokenPrice instance.
    /// </summary>
    public static TokenPrice Create(
        string tokenAddress,
        string quoteTokenAddress,
        string quoteTokenSymbol,
        decimal price,
        decimal priceUsd,
        string poolAddress,
        decimal liquidity)
    {
        return new TokenPrice
        {
            TokenAddress = tokenAddress.ToLowerInvariant(),
            QuoteTokenAddress = quoteTokenAddress.ToLowerInvariant(),
            QuoteTokenSymbol = quoteTokenSymbol,
            Price = price,
            PriceUsd = priceUsd,
            Timestamp = DateTime.UtcNow,
            PoolAddress = poolAddress.ToLowerInvariant(),
            Liquidity = liquidity
        };
    }
}

/// <summary>
/// Represents a price point for TWAP calculation.
/// </summary>
public sealed record PricePoint
{
    public decimal Price { get; init; }
    public DateTime Timestamp { get; init; }
    public decimal Volume { get; init; }
}

/// <summary>
/// Represents TWAP calculation result.
/// </summary>
public sealed record TwapResult
{
    public string TokenAddress { get; init; } = string.Empty;
    public string QuoteTokenSymbol { get; init; } = string.Empty;
    public decimal TwapPrice { get; init; }
    public decimal SpotPrice { get; init; }
    public decimal PriceDeviation { get; init; }
    public TimeSpan Period { get; init; }
    public int DataPoints { get; init; }
    public DateTime CalculatedAt { get; init; }
}
