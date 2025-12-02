using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Events;
using AnalyzerCore.Infrastructure.RealTime;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Application.EventHandlers;

/// <summary>
/// Handles the LargeArbitrageAlertEvent.
/// Sends high-priority alerts for large arbitrage opportunities.
/// </summary>
public sealed class LargeArbitrageAlertDomainEventHandler : IDomainEventHandler<LargeArbitrageAlertEvent>
{
    private readonly ILogger<LargeArbitrageAlertDomainEventHandler> _logger;
    private readonly IRealtimeNotificationService _notificationService;

    public LargeArbitrageAlertDomainEventHandler(
        ILogger<LargeArbitrageAlertDomainEventHandler> logger,
        IRealtimeNotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(LargeArbitrageAlertEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "LARGE ARBITRAGE ALERT: {TokenSymbol} - Net Profit: ${NetProfit:F2}, Spread: {SpreadPercent:F2}%, Confidence: {Confidence}%",
            notification.TokenSymbol,
            notification.NetProfitUsd,
            notification.SpreadPercent,
            notification.ConfidenceScore);

        // Broadcast high-priority alert
        await _notificationService.BroadcastAlertAsync(new AlertMessage
        {
            Type = "arbitrage_alert",
            Severity = "critical",
            Title = "Large Arbitrage Opportunity Detected",
            Message = $"{notification.TokenSymbol}: Potential profit of ${notification.NetProfitUsd:F2} with {notification.SpreadPercent:F2}% spread",
            Data = new Dictionary<string, object>
            {
                ["opportunityId"] = notification.OpportunityId,
                ["tokenAddress"] = notification.TokenAddress,
                ["tokenSymbol"] = notification.TokenSymbol,
                ["netProfitUsd"] = notification.NetProfitUsd,
                ["spreadPercent"] = notification.SpreadPercent,
                ["confidenceScore"] = notification.ConfidenceScore
            },
            Timestamp = notification.OccurredAt
        });
    }
}
