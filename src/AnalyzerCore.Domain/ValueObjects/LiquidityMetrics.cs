namespace AnalyzerCore.Domain.ValueObjects;

/// <summary>
/// Comprehensive liquidity metrics for a pool or token.
/// </summary>
public sealed record LiquidityMetrics
{
    /// <summary>
    /// Pool address.
    /// </summary>
    public string PoolAddress { get; init; } = string.Empty;

    /// <summary>
    /// Token 0 address.
    /// </summary>
    public string Token0Address { get; init; } = string.Empty;

    /// <summary>
    /// Token 0 symbol.
    /// </summary>
    public string Token0Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Token 1 address.
    /// </summary>
    public string Token1Address { get; init; } = string.Empty;

    /// <summary>
    /// Token 1 symbol.
    /// </summary>
    public string Token1Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Total Value Locked in USD.
    /// </summary>
    public decimal TvlUsd { get; init; }

    /// <summary>
    /// Reserve of token 0.
    /// </summary>
    public decimal Reserve0 { get; init; }

    /// <summary>
    /// Reserve of token 1.
    /// </summary>
    public decimal Reserve1 { get; init; }

    /// <summary>
    /// Reserve of token 0 in USD.
    /// </summary>
    public decimal Reserve0Usd { get; init; }

    /// <summary>
    /// Reserve of token 1 in USD.
    /// </summary>
    public decimal Reserve1Usd { get; init; }

    /// <summary>
    /// 24h trading volume in USD.
    /// </summary>
    public decimal Volume24hUsd { get; init; }

    /// <summary>
    /// 24h fees earned in USD.
    /// </summary>
    public decimal Fees24hUsd { get; init; }

    /// <summary>
    /// Annual Percentage Rate from fees.
    /// </summary>
    public decimal AprPercent { get; init; }

    /// <summary>
    /// Liquidity depth score (0-100).
    /// </summary>
    public int DepthScore { get; init; }

    /// <summary>
    /// Timestamp of the metrics.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Creates liquidity metrics.
    /// </summary>
    public static LiquidityMetrics Create(
        string poolAddress,
        string token0Address,
        string token0Symbol,
        string token1Address,
        string token1Symbol,
        decimal reserve0,
        decimal reserve1,
        decimal reserve0Usd,
        decimal reserve1Usd,
        decimal volume24hUsd,
        decimal feePercent = 0.3m)
    {
        var tvlUsd = reserve0Usd + reserve1Usd;
        var fees24hUsd = volume24hUsd * (feePercent / 100);
        var aprPercent = tvlUsd > 0 ? (fees24hUsd * 365 / tvlUsd) * 100 : 0;
        var depthScore = CalculateDepthScore(tvlUsd, volume24hUsd);

        return new LiquidityMetrics
        {
            PoolAddress = poolAddress.ToLowerInvariant(),
            Token0Address = token0Address.ToLowerInvariant(),
            Token0Symbol = token0Symbol.ToUpperInvariant(),
            Token1Address = token1Address.ToLowerInvariant(),
            Token1Symbol = token1Symbol.ToUpperInvariant(),
            TvlUsd = tvlUsd,
            Reserve0 = reserve0,
            Reserve1 = reserve1,
            Reserve0Usd = reserve0Usd,
            Reserve1Usd = reserve1Usd,
            Volume24hUsd = volume24hUsd,
            Fees24hUsd = fees24hUsd,
            AprPercent = aprPercent,
            DepthScore = depthScore,
            Timestamp = DateTime.UtcNow
        };
    }

    private static int CalculateDepthScore(decimal tvlUsd, decimal volume24hUsd)
    {
        var score = 0;

        // TVL component (max 50 points)
        if (tvlUsd >= 10_000_000) score += 50;
        else if (tvlUsd >= 1_000_000) score += 40;
        else if (tvlUsd >= 100_000) score += 30;
        else if (tvlUsd >= 10_000) score += 20;
        else if (tvlUsd >= 1_000) score += 10;

        // Volume/TVL ratio component (max 30 points)
        var volumeRatio = tvlUsd > 0 ? volume24hUsd / tvlUsd : 0;
        if (volumeRatio >= 0.5m) score += 30;
        else if (volumeRatio >= 0.1m) score += 20;
        else if (volumeRatio >= 0.01m) score += 10;

        // Balance component (max 20 points) - simplified as we don't have USD values here
        score += 15; // Default to reasonably balanced

        return Math.Min(100, score);
    }
}

/// <summary>
/// Impermanent loss calculation result.
/// </summary>
public sealed record ImpermanentLossResult
{
    /// <summary>
    /// Pool address.
    /// </summary>
    public string PoolAddress { get; init; } = string.Empty;

    /// <summary>
    /// Initial price ratio (token1/token0).
    /// </summary>
    public decimal InitialPriceRatio { get; init; }

    /// <summary>
    /// Current price ratio (token1/token0).
    /// </summary>
    public decimal CurrentPriceRatio { get; init; }

    /// <summary>
    /// Price change percentage.
    /// </summary>
    public decimal PriceChangePercent { get; init; }

    /// <summary>
    /// Impermanent loss percentage (negative value).
    /// </summary>
    public decimal ImpermanentLossPercent { get; init; }

    /// <summary>
    /// Value if held in wallet instead of LP.
    /// </summary>
    public decimal HodlValueUsd { get; init; }

    /// <summary>
    /// Current LP position value.
    /// </summary>
    public decimal LpValueUsd { get; init; }

    /// <summary>
    /// Difference (LP - HODL).
    /// </summary>
    public decimal DifferenceUsd { get; init; }

    /// <summary>
    /// Timestamp of calculation.
    /// </summary>
    public DateTime CalculatedAt { get; init; }

    /// <summary>
    /// Calculates impermanent loss.
    /// </summary>
    public static ImpermanentLossResult Calculate(
        string poolAddress,
        decimal initialPriceRatio,
        decimal currentPriceRatio,
        decimal initialInvestmentUsd)
    {
        // IL formula: IL = 2 * sqrt(priceRatio) / (1 + priceRatio) - 1
        var priceRatio = currentPriceRatio / initialPriceRatio;
        var sqrtRatio = (decimal)Math.Sqrt((double)priceRatio);
        var ilFactor = (2 * sqrtRatio) / (1 + priceRatio);
        var ilPercent = (ilFactor - 1) * 100;

        // Calculate values
        var priceChangePercent = ((currentPriceRatio - initialPriceRatio) / initialPriceRatio) * 100;

        // Simplified HODL value calculation (assumes 50/50 split)
        var hodlValue = initialInvestmentUsd * (1 + priceChangePercent / 200);
        var lpValue = initialInvestmentUsd * ilFactor;

        return new ImpermanentLossResult
        {
            PoolAddress = poolAddress.ToLowerInvariant(),
            InitialPriceRatio = initialPriceRatio,
            CurrentPriceRatio = currentPriceRatio,
            PriceChangePercent = priceChangePercent,
            ImpermanentLossPercent = ilPercent,
            HodlValueUsd = hodlValue,
            LpValueUsd = lpValue,
            DifferenceUsd = lpValue - hodlValue,
            CalculatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Token liquidity summary across all pools.
/// </summary>
public sealed record TokenLiquiditySummary
{
    /// <summary>
    /// Token address.
    /// </summary>
    public string TokenAddress { get; init; } = string.Empty;

    /// <summary>
    /// Token symbol.
    /// </summary>
    public string TokenSymbol { get; init; } = string.Empty;

    /// <summary>
    /// Total liquidity across all pools in USD.
    /// </summary>
    public decimal TotalLiquidityUsd { get; init; }

    /// <summary>
    /// Number of pools containing this token.
    /// </summary>
    public int PoolCount { get; init; }

    /// <summary>
    /// Top pools by liquidity.
    /// </summary>
    public IReadOnlyList<PoolLiquiditySummary> TopPools { get; init; } = Array.Empty<PoolLiquiditySummary>();

    /// <summary>
    /// Average liquidity per pool.
    /// </summary>
    public decimal AverageLiquidityPerPool { get; init; }

    /// <summary>
    /// 24h total volume across all pools.
    /// </summary>
    public decimal TotalVolume24hUsd { get; init; }

    /// <summary>
    /// Timestamp of the summary.
    /// </summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Summary of a pool's liquidity.
/// </summary>
public sealed record PoolLiquiditySummary
{
    /// <summary>
    /// Pool address.
    /// </summary>
    public string PoolAddress { get; init; } = string.Empty;

    /// <summary>
    /// Paired token address.
    /// </summary>
    public string PairedTokenAddress { get; init; } = string.Empty;

    /// <summary>
    /// Paired token symbol.
    /// </summary>
    public string PairedTokenSymbol { get; init; } = string.Empty;

    /// <summary>
    /// Liquidity in USD.
    /// </summary>
    public decimal LiquidityUsd { get; init; }

    /// <summary>
    /// Share of total liquidity (percentage).
    /// </summary>
    public decimal SharePercent { get; init; }
}
