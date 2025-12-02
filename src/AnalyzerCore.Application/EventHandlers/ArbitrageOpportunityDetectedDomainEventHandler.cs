using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Events;
using AnalyzerCore.Infrastructure.RealTime;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Application.EventHandlers;

/// <summary>
/// Handles the ArbitrageOpportunityDetectedEvent.
/// Broadcasts arbitrage opportunities to subscribed traders and analytics systems.
/// </summary>
public sealed class ArbitrageOpportunityDetectedDomainEventHandler : IDomainEventHandler<ArbitrageOpportunityDetectedEvent>
{
    private readonly ILogger<ArbitrageOpportunityDetectedDomainEventHandler> _logger;
    private readonly IRealtimeNotificationService _notificationService;

    public ArbitrageOpportunityDetectedDomainEventHandler(
        ILogger<ArbitrageOpportunityDetectedDomainEventHandler> logger,
        IRealtimeNotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(ArbitrageOpportunityDetectedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Arbitrage opportunity detected: {TokenSymbol} - Spread: {SpreadPercent:F2}%, Expected Profit: ${ExpectedProfit:F2}, Net: ${NetProfit:F2}",
            notification.TokenSymbol,
            notification.SpreadPercent,
            notification.ExpectedProfitUsd,
            notification.NetProfitUsd);

        // Broadcast to arbitrage subscribers
        await _notificationService.BroadcastArbitrageOpportunityAsync(new ArbitrageOpportunityMessage
        {
            OpportunityId = notification.OpportunityId,
            TokenAddress = notification.TokenAddress,
            TokenSymbol = notification.TokenSymbol,
            SpreadPercent = notification.SpreadPercent,
            ExpectedProfitUsd = notification.ExpectedProfitUsd,
            NetProfitUsd = notification.NetProfitUsd,
            PathLength = notification.PathLength,
            DetectedAt = notification.OccurredAt
        });
    }
}
