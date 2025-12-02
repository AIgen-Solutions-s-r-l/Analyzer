namespace AnalyzerCore.Api.Hubs;

/// <summary>
/// Message sent when a new block is mined.
/// </summary>
public sealed record BlockUpdateMessage
{
    public long BlockNumber { get; init; }
    public string BlockHash { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Type { get; init; } = "NewBlock";
}

/// <summary>
/// Message sent when pool reserves are updated.
/// </summary>
public sealed record PoolUpdateMessage
{
    public string Address { get; init; } = string.Empty;
    public string Token0Address { get; init; } = string.Empty;
    public string Token1Address { get; init; } = string.Empty;
    public decimal Reserve0 { get; init; }
    public decimal Reserve1 { get; init; }
    public string Factory { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public string Type { get; init; } = "PoolUpdate";
}

/// <summary>
/// Message sent when a token price is updated.
/// </summary>
public sealed record PriceUpdateMessage
{
    public string TokenAddress { get; init; } = string.Empty;
    public string QuoteTokenAddress { get; init; } = string.Empty;
    public string QuoteTokenSymbol { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal PriceUsd { get; init; }
    public string PoolAddress { get; init; } = string.Empty;
    public decimal Liquidity { get; init; }
    public DateTime Timestamp { get; init; }
    public string Type { get; init; } = "PriceUpdate";
}

/// <summary>
/// Message sent when a new token is discovered.
/// </summary>
public sealed record NewTokenMessage
{
    public string Address { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Decimals { get; init; }
    public string ChainId { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Type { get; init; } = "NewToken";
}

/// <summary>
/// Message sent when a new pool is discovered.
/// </summary>
public sealed record NewPoolMessage
{
    public string Address { get; init; } = string.Empty;
    public string Token0Address { get; init; } = string.Empty;
    public string Token1Address { get; init; } = string.Empty;
    public string Factory { get; init; } = string.Empty;
    public decimal Reserve0 { get; init; }
    public decimal Reserve1 { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Type { get; init; } = "NewPool";
}

/// <summary>
/// Message sent when an arbitrage opportunity is detected.
/// </summary>
public sealed record ArbitrageAlertMessage
{
    public Guid Id { get; init; }
    public string TokenAddress { get; init; } = string.Empty;
    public string TokenSymbol { get; init; } = string.Empty;
    public decimal BuyPrice { get; init; }
    public decimal SellPrice { get; init; }
    public decimal SpreadPercent { get; init; }
    public decimal ExpectedProfitUsd { get; init; }
    public decimal NetProfitUsd { get; init; }
    public decimal RoiPercent { get; init; }
    public bool IsProfitable { get; init; }
    public int ConfidenceScore { get; init; }
    public int PathLength { get; init; }
    public DateTime DetectedAt { get; init; }
    public string Type { get; init; } = "ArbitrageOpportunity";
}

/// <summary>
/// Message sent when a significant price change occurs.
/// </summary>
public sealed record SignificantPriceChangeMessage
{
    public string TokenAddress { get; init; } = string.Empty;
    public string TokenSymbol { get; init; } = string.Empty;
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public decimal ChangePercent { get; init; }
    public string Direction { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Type { get; init; } = "SignificantPriceChange";
}
