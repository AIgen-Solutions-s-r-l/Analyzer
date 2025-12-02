namespace AnalyzerCore.Api.Contracts.Liquidity;

/// <summary>
/// Response containing pool liquidity metrics.
/// </summary>
public sealed record LiquidityMetricsResponse
{
    public string PoolAddress { get; init; } = string.Empty;
    public string Token0Address { get; init; } = string.Empty;
    public string Token0Symbol { get; init; } = string.Empty;
    public string Token1Address { get; init; } = string.Empty;
    public string Token1Symbol { get; init; } = string.Empty;
    public decimal TvlUsd { get; init; }
    public decimal Reserve0 { get; init; }
    public decimal Reserve1 { get; init; }
    public decimal Reserve0Usd { get; init; }
    public decimal Reserve1Usd { get; init; }
    public decimal Volume24hUsd { get; init; }
    public decimal Fees24hUsd { get; init; }
    public decimal AprPercent { get; init; }
    public int DepthScore { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Response containing token liquidity summary.
/// </summary>
public sealed record TokenLiquiditySummaryResponse
{
    public string TokenAddress { get; init; } = string.Empty;
    public string TokenSymbol { get; init; } = string.Empty;
    public decimal TotalLiquidityUsd { get; init; }
    public int PoolCount { get; init; }
    public IReadOnlyList<PoolLiquiditySummaryResponse> TopPools { get; init; } = Array.Empty<PoolLiquiditySummaryResponse>();
    public decimal AverageLiquidityPerPool { get; init; }
    public decimal TotalVolume24hUsd { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Response containing pool liquidity summary.
/// </summary>
public sealed record PoolLiquiditySummaryResponse
{
    public string PoolAddress { get; init; } = string.Empty;
    public string PairedTokenAddress { get; init; } = string.Empty;
    public string PairedTokenSymbol { get; init; } = string.Empty;
    public decimal LiquidityUsd { get; init; }
    public decimal SharePercent { get; init; }
}

/// <summary>
/// Response containing impermanent loss calculation.
/// </summary>
public sealed record ImpermanentLossResponse
{
    public string PoolAddress { get; init; } = string.Empty;
    public decimal InitialPriceRatio { get; init; }
    public decimal CurrentPriceRatio { get; init; }
    public decimal PriceChangePercent { get; init; }
    public decimal ImpermanentLossPercent { get; init; }
    public decimal HodlValueUsd { get; init; }
    public decimal LpValueUsd { get; init; }
    public decimal DifferenceUsd { get; init; }
    public DateTime CalculatedAt { get; init; }
}

/// <summary>
/// Response containing liquidity concentration analysis.
/// </summary>
public sealed record LiquidityConcentrationResponse
{
    public string TokenAddress { get; init; } = string.Empty;
    public decimal TotalLiquidityUsd { get; init; }
    public decimal TopPoolConcentration { get; init; }
    public decimal Top3PoolsConcentration { get; init; }
    public decimal Top5PoolsConcentration { get; init; }
    public decimal HhiIndex { get; init; }
    public string ConcentrationLevel { get; init; } = string.Empty;
    public int PoolCount { get; init; }
}

/// <summary>
/// Request for impermanent loss calculation.
/// </summary>
public sealed record ImpermanentLossRequest
{
    /// <summary>
    /// Pool address.
    /// </summary>
    public string PoolAddress { get; init; } = string.Empty;

    /// <summary>
    /// Price ratio at entry (token1/token0).
    /// </summary>
    public decimal EntryPriceRatio { get; init; }

    /// <summary>
    /// Initial investment in USD.
    /// </summary>
    public decimal InitialInvestmentUsd { get; init; }
}
