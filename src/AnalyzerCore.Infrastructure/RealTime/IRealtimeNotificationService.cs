using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Infrastructure.RealTime;

/// <summary>
/// Service interface for sending real-time notifications.
/// </summary>
public interface IRealtimeNotificationService
{
    /// <summary>
    /// Notifies clients of a new block.
    /// </summary>
    Task NotifyNewBlockAsync(long blockNumber, string blockHash, DateTime timestamp);

    /// <summary>
    /// Notifies clients of pool reserve updates.
    /// </summary>
    Task NotifyPoolUpdateAsync(Pool pool);

    /// <summary>
    /// Notifies clients of a price change.
    /// </summary>
    Task NotifyPriceUpdateAsync(TokenPrice price);

    /// <summary>
    /// Notifies clients of a new token discovery.
    /// </summary>
    Task NotifyNewTokenAsync(Token token);

    /// <summary>
    /// Notifies clients of a new pool discovery.
    /// </summary>
    Task NotifyNewPoolAsync(Pool pool);

    /// <summary>
    /// Notifies clients of an arbitrage opportunity.
    /// </summary>
    Task NotifyArbitrageOpportunityAsync(ArbitrageOpportunity opportunity);

    /// <summary>
    /// Notifies clients of a significant price change.
    /// </summary>
    Task NotifySignificantPriceChangeAsync(
        string tokenAddress,
        string tokenSymbol,
        decimal oldPrice,
        decimal newPrice,
        decimal changePercent);

    /// <summary>
    /// Broadcasts a price update message to token subscribers.
    /// </summary>
    Task BroadcastPriceUpdateAsync(PriceUpdateMessage message);

    /// <summary>
    /// Broadcasts an alert message to all connected clients.
    /// </summary>
    Task BroadcastAlertAsync(AlertMessage message);

    /// <summary>
    /// Broadcasts an arbitrage opportunity message to arbitrage subscribers.
    /// </summary>
    Task BroadcastArbitrageOpportunityAsync(ArbitrageOpportunityMessage message);
}

/// <summary>
/// Message for price update broadcasts.
/// </summary>
public sealed record PriceUpdateMessage
{
    public string TokenAddress { get; init; } = string.Empty;
    public string TokenSymbol { get; init; } = string.Empty;
    public string QuoteTokenSymbol { get; init; } = string.Empty;
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public decimal PriceChangePercent { get; init; }
    public decimal PriceUsd { get; init; }
    public string PoolAddress { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Message for alert broadcasts.
/// </summary>
public sealed record AlertMessage
{
    public string Type { get; init; } = string.Empty;
    public string Severity { get; init; } = "info";
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, object> Data { get; init; } = new();
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Message for arbitrage opportunity broadcasts.
/// </summary>
public sealed record ArbitrageOpportunityMessage
{
    public Guid OpportunityId { get; init; }
    public string TokenAddress { get; init; } = string.Empty;
    public string TokenSymbol { get; init; } = string.Empty;
    public decimal SpreadPercent { get; init; }
    public decimal ExpectedProfitUsd { get; init; }
    public decimal NetProfitUsd { get; init; }
    public int PathLength { get; init; }
    public DateTime DetectedAt { get; init; }
}
