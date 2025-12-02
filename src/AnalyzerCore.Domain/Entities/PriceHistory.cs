using AnalyzerCore.Domain.Abstractions;

namespace AnalyzerCore.Domain.Entities;

/// <summary>
/// Entity representing historical price data for a token.
/// </summary>
public sealed class PriceHistory : Entity
{
    public string TokenAddress { get; private set; } = string.Empty;
    public string QuoteTokenAddress { get; private set; } = string.Empty;
    public string QuoteTokenSymbol { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public decimal PriceUsd { get; private set; }
    public string PoolAddress { get; private set; } = string.Empty;
    public decimal Reserve0 { get; private set; }
    public decimal Reserve1 { get; private set; }
    public decimal Liquidity { get; private set; }
    public long BlockNumber { get; private set; }
    public DateTime Timestamp { get; private set; }

    private PriceHistory() { }

    /// <summary>
    /// Creates a new price history entry.
    /// </summary>
    public static PriceHistory Create(
        string tokenAddress,
        string quoteTokenAddress,
        string quoteTokenSymbol,
        decimal price,
        decimal priceUsd,
        string poolAddress,
        decimal reserve0,
        decimal reserve1,
        decimal liquidity,
        long blockNumber,
        DateTime timestamp)
    {
        return new PriceHistory
        {
            Id = Guid.NewGuid(),
            TokenAddress = tokenAddress.ToLowerInvariant(),
            QuoteTokenAddress = quoteTokenAddress.ToLowerInvariant(),
            QuoteTokenSymbol = quoteTokenSymbol.ToUpperInvariant(),
            Price = price,
            PriceUsd = priceUsd,
            PoolAddress = poolAddress.ToLowerInvariant(),
            Reserve0 = reserve0,
            Reserve1 = reserve1,
            Liquidity = liquidity,
            BlockNumber = blockNumber,
            Timestamp = timestamp,
            CreatedAt = DateTime.UtcNow
        };
    }
}
