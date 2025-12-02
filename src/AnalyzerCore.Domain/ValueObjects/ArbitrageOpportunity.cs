namespace AnalyzerCore.Domain.ValueObjects;

/// <summary>
/// Represents an arbitrage opportunity between two or more pools.
/// </summary>
public sealed record ArbitrageOpportunity
{
    /// <summary>
    /// Unique identifier for the opportunity.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The token being arbitraged.
    /// </summary>
    public string TokenAddress { get; init; } = string.Empty;

    /// <summary>
    /// Token symbol for display.
    /// </summary>
    public string TokenSymbol { get; init; } = string.Empty;

    /// <summary>
    /// The path of pools for the arbitrage.
    /// </summary>
    public IReadOnlyList<ArbitrageLeg> Path { get; init; } = Array.Empty<ArbitrageLeg>();

    /// <summary>
    /// Price in the buy pool.
    /// </summary>
    public decimal BuyPrice { get; init; }

    /// <summary>
    /// Price in the sell pool.
    /// </summary>
    public decimal SellPrice { get; init; }

    /// <summary>
    /// Price difference as percentage.
    /// </summary>
    public decimal SpreadPercent { get; init; }

    /// <summary>
    /// Expected profit in USD for optimal input amount.
    /// </summary>
    public decimal ExpectedProfitUsd { get; init; }

    /// <summary>
    /// Optimal input amount for maximum profit.
    /// </summary>
    public decimal OptimalInputAmount { get; init; }

    /// <summary>
    /// Estimated gas cost in USD.
    /// </summary>
    public decimal EstimatedGasCostUsd { get; init; }

    /// <summary>
    /// Net profit after gas costs.
    /// </summary>
    public decimal NetProfitUsd { get; init; }

    /// <summary>
    /// Return on investment percentage.
    /// </summary>
    public decimal RoiPercent { get; init; }

    /// <summary>
    /// Whether this opportunity is profitable after gas.
    /// </summary>
    public bool IsProfitable { get; init; }

    /// <summary>
    /// Confidence score (0-100) based on liquidity and data freshness.
    /// </summary>
    public int ConfidenceScore { get; init; }

    /// <summary>
    /// When this opportunity was detected.
    /// </summary>
    public DateTime DetectedAt { get; init; }

    /// <summary>
    /// Estimated time until the opportunity may disappear.
    /// </summary>
    public TimeSpan? EstimatedLifespan { get; init; }

    /// <summary>
    /// Creates a new arbitrage opportunity.
    /// </summary>
    public static ArbitrageOpportunity Create(
        string tokenAddress,
        string tokenSymbol,
        IReadOnlyList<ArbitrageLeg> path,
        decimal buyPrice,
        decimal sellPrice,
        decimal optimalInputAmount,
        decimal expectedProfitUsd,
        decimal estimatedGasCostUsd,
        int confidenceScore)
    {
        var spreadPercent = buyPrice > 0
            ? ((sellPrice - buyPrice) / buyPrice) * 100
            : 0;
        var netProfit = expectedProfitUsd - estimatedGasCostUsd;
        var roiPercent = optimalInputAmount > 0
            ? (netProfit / optimalInputAmount) * 100
            : 0;

        return new ArbitrageOpportunity
        {
            Id = Guid.NewGuid(),
            TokenAddress = tokenAddress.ToLowerInvariant(),
            TokenSymbol = tokenSymbol.ToUpperInvariant(),
            Path = path,
            BuyPrice = buyPrice,
            SellPrice = sellPrice,
            SpreadPercent = spreadPercent,
            OptimalInputAmount = optimalInputAmount,
            ExpectedProfitUsd = expectedProfitUsd,
            EstimatedGasCostUsd = estimatedGasCostUsd,
            NetProfitUsd = netProfit,
            RoiPercent = roiPercent,
            IsProfitable = netProfit > 0,
            ConfidenceScore = confidenceScore,
            DetectedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Represents one leg of an arbitrage path.
/// </summary>
public sealed record ArbitrageLeg
{
    /// <summary>
    /// The pool address for this leg.
    /// </summary>
    public string PoolAddress { get; init; } = string.Empty;

    /// <summary>
    /// The DEX/protocol name (e.g., "Uniswap V2", "SushiSwap").
    /// </summary>
    public string DexName { get; init; } = string.Empty;

    /// <summary>
    /// Token being sold in this leg.
    /// </summary>
    public string TokenIn { get; init; } = string.Empty;

    /// <summary>
    /// Token being bought in this leg.
    /// </summary>
    public string TokenOut { get; init; } = string.Empty;

    /// <summary>
    /// The exchange rate for this leg.
    /// </summary>
    public decimal Rate { get; init; }

    /// <summary>
    /// Available liquidity in this pool.
    /// </summary>
    public decimal Liquidity { get; init; }

    /// <summary>
    /// Estimated price impact for this leg.
    /// </summary>
    public decimal PriceImpact { get; init; }
}

/// <summary>
/// Type of arbitrage opportunity.
/// </summary>
public enum ArbitrageType
{
    /// <summary>
    /// Simple two-pool arbitrage.
    /// </summary>
    TwoPool,

    /// <summary>
    /// Triangular arbitrage through three pools.
    /// </summary>
    Triangular,

    /// <summary>
    /// Multi-hop arbitrage through multiple pools.
    /// </summary>
    MultiHop,

    /// <summary>
    /// Cross-DEX arbitrage.
    /// </summary>
    CrossDex
}
