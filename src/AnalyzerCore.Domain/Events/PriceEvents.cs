using AnalyzerCore.Domain.Abstractions;

namespace AnalyzerCore.Domain.Events;

/// <summary>
/// Event raised when a token price is updated.
/// </summary>
public sealed record PriceUpdatedEvent : IDomainEvent
{
    public string TokenAddress { get; init; } = string.Empty;
    public string TokenSymbol { get; init; } = string.Empty;
    public string QuoteTokenSymbol { get; init; } = string.Empty;
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public decimal PriceChangePercent { get; init; }
    public decimal PriceUsd { get; init; }
    public string PoolAddress { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
}

/// <summary>
/// Event raised when a significant price change is detected.
/// </summary>
public sealed record SignificantPriceChangeEvent : IDomainEvent
{
    public string TokenAddress { get; init; } = string.Empty;
    public string TokenSymbol { get; init; } = string.Empty;
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public decimal PriceChangePercent { get; init; }
    public TimeSpan TimePeriod { get; init; }
    public DateTime OccurredAt { get; init; }
}

/// <summary>
/// Event raised when price data becomes stale.
/// </summary>
public sealed record PriceStaleEvent : IDomainEvent
{
    public string TokenAddress { get; init; } = string.Empty;
    public DateTime LastUpdateTime { get; init; }
    public TimeSpan StaleDuration { get; init; }
    public DateTime OccurredAt { get; init; }
}
