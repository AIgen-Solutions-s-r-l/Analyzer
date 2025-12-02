using AnalyzerCore.Domain.Abstractions;

namespace AnalyzerCore.Domain.Events;

/// <summary>
/// Event raised when a profitable arbitrage opportunity is detected.
/// </summary>
public sealed record ArbitrageOpportunityDetectedEvent : IDomainEvent
{
    public Guid OpportunityId { get; init; }
    public string TokenAddress { get; init; } = string.Empty;
    public string TokenSymbol { get; init; } = string.Empty;
    public decimal SpreadPercent { get; init; }
    public decimal ExpectedProfitUsd { get; init; }
    public decimal NetProfitUsd { get; init; }
    public int PathLength { get; init; }
    public DateTime OccurredAt { get; init; }
}

/// <summary>
/// Event raised when an arbitrage opportunity disappears.
/// </summary>
public sealed record ArbitrageOpportunityExpiredEvent : IDomainEvent
{
    public Guid OpportunityId { get; init; }
    public string TokenAddress { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
}

/// <summary>
/// Event raised when a large arbitrage opportunity is detected (alert threshold).
/// </summary>
public sealed record LargeArbitrageAlertEvent : IDomainEvent
{
    public Guid OpportunityId { get; init; }
    public string TokenAddress { get; init; } = string.Empty;
    public string TokenSymbol { get; init; } = string.Empty;
    public decimal NetProfitUsd { get; init; }
    public decimal SpreadPercent { get; init; }
    public int ConfidenceScore { get; init; }
    public DateTime OccurredAt { get; init; }
}
