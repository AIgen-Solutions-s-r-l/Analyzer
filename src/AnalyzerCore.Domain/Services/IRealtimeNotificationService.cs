namespace AnalyzerCore.Domain.Services;

/// <summary>
/// Domain service interface for real-time notifications.
/// Used by domain event handlers to notify external systems of domain events.
/// </summary>
public interface IRealtimeNotificationService
{
    /// <summary>
    /// Notifies clients about a token info update.
    /// </summary>
    Task NotifyTokenUpdatedAsync(
        string tokenAddress,
        string newSymbol,
        string newName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies clients of a significant price change.
    /// </summary>
    Task NotifySignificantPriceChangeAsync(
        string tokenAddress,
        string tokenSymbol,
        decimal oldPrice,
        decimal newPrice,
        decimal changePercent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies clients of an arbitrage opportunity.
    /// </summary>
    Task NotifyArbitrageOpportunityAsync(
        string tokenAddress,
        string tokenSymbol,
        decimal profitUsd,
        decimal spreadPercent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies clients of a large arbitrage alert.
    /// </summary>
    Task NotifyLargeArbitrageAlertAsync(
        string tokenAddress,
        string tokenSymbol,
        decimal profitUsd,
        decimal spreadPercent,
        CancellationToken cancellationToken = default);
}
