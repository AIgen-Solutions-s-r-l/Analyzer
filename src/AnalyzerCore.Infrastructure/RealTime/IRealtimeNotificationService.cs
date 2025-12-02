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
}
