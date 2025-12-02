namespace AnalyzerCore.Api.Contracts.Arbitrage;

/// <summary>
/// Response containing an arbitrage opportunity.
/// </summary>
public sealed record ArbitrageOpportunityResponse
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Token address being arbitraged.
    /// </summary>
    public string TokenAddress { get; init; } = string.Empty;

    /// <summary>
    /// Token symbol.
    /// </summary>
    public string TokenSymbol { get; init; } = string.Empty;

    /// <summary>
    /// The arbitrage path.
    /// </summary>
    public IReadOnlyList<ArbitrageLegResponse> Path { get; init; } = Array.Empty<ArbitrageLegResponse>();

    /// <summary>
    /// Buy price.
    /// </summary>
    public decimal BuyPrice { get; init; }

    /// <summary>
    /// Sell price.
    /// </summary>
    public decimal SellPrice { get; init; }

    /// <summary>
    /// Price spread percentage.
    /// </summary>
    public decimal SpreadPercent { get; init; }

    /// <summary>
    /// Expected profit in USD.
    /// </summary>
    public decimal ExpectedProfitUsd { get; init; }

    /// <summary>
    /// Optimal input amount.
    /// </summary>
    public decimal OptimalInputAmount { get; init; }

    /// <summary>
    /// Estimated gas cost in USD.
    /// </summary>
    public decimal EstimatedGasCostUsd { get; init; }

    /// <summary>
    /// Net profit after gas.
    /// </summary>
    public decimal NetProfitUsd { get; init; }

    /// <summary>
    /// Return on investment percentage.
    /// </summary>
    public decimal RoiPercent { get; init; }

    /// <summary>
    /// Whether this is profitable after gas.
    /// </summary>
    public bool IsProfitable { get; init; }

    /// <summary>
    /// Confidence score (0-100).
    /// </summary>
    public int ConfidenceScore { get; init; }

    /// <summary>
    /// When detected.
    /// </summary>
    public DateTime DetectedAt { get; init; }
}

/// <summary>
/// Response for an arbitrage leg.
/// </summary>
public sealed record ArbitrageLegResponse
{
    /// <summary>
    /// Pool address.
    /// </summary>
    public string PoolAddress { get; init; } = string.Empty;

    /// <summary>
    /// DEX name.
    /// </summary>
    public string DexName { get; init; } = string.Empty;

    /// <summary>
    /// Token being sold.
    /// </summary>
    public string TokenIn { get; init; } = string.Empty;

    /// <summary>
    /// Token being bought.
    /// </summary>
    public string TokenOut { get; init; } = string.Empty;

    /// <summary>
    /// Exchange rate.
    /// </summary>
    public decimal Rate { get; init; }

    /// <summary>
    /// Pool liquidity.
    /// </summary>
    public decimal Liquidity { get; init; }
}
