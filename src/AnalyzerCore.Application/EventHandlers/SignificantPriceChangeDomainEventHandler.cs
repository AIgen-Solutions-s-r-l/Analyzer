using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Events;
using AnalyzerCore.Infrastructure.RealTime;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Application.EventHandlers;

/// <summary>
/// Handles the SignificantPriceChangeEvent.
/// Sends alerts for large price movements that may indicate market events.
/// </summary>
public sealed class SignificantPriceChangeDomainEventHandler : IDomainEventHandler<SignificantPriceChangeEvent>
{
    private readonly ILogger<SignificantPriceChangeDomainEventHandler> _logger;
    private readonly IRealtimeNotificationService _notificationService;

    // Alert thresholds
    private const decimal WarningThreshold = 5m;  // 5% change
    private const decimal CriticalThreshold = 10m; // 10% change

    public SignificantPriceChangeDomainEventHandler(
        ILogger<SignificantPriceChangeDomainEventHandler> logger,
        IRealtimeNotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(SignificantPriceChangeEvent notification, CancellationToken cancellationToken)
    {
        var severity = GetSeverity(Math.Abs(notification.PriceChangePercent));
        var direction = notification.PriceChangePercent > 0 ? "increase" : "decrease";

        _logger.Log(
            severity == "critical" ? LogLevel.Warning : LogLevel.Information,
            "Significant price {Direction} detected for {TokenSymbol}: {ChangePercent:F2}% over {Period}",
            direction,
            notification.TokenSymbol,
            notification.PriceChangePercent,
            notification.TimePeriod);

        // Broadcast alert to subscribed clients
        await _notificationService.BroadcastAlertAsync(new AlertMessage
        {
            Type = "price_alert",
            Severity = severity,
            Title = $"Significant Price {(notification.PriceChangePercent > 0 ? "Increase" : "Decrease")}",
            Message = $"{notification.TokenSymbol} price changed by {notification.PriceChangePercent:F2}% in {notification.TimePeriod.TotalMinutes:F0} minutes",
            Data = new Dictionary<string, object>
            {
                ["tokenAddress"] = notification.TokenAddress,
                ["tokenSymbol"] = notification.TokenSymbol,
                ["oldPrice"] = notification.OldPrice,
                ["newPrice"] = notification.NewPrice,
                ["changePercent"] = notification.PriceChangePercent,
                ["timePeriodMinutes"] = notification.TimePeriod.TotalMinutes
            },
            Timestamp = notification.OccurredAt
        });
    }

    private static string GetSeverity(decimal changePercent)
    {
        if (changePercent >= CriticalThreshold) return "critical";
        if (changePercent >= WarningThreshold) return "warning";
        return "info";
    }
}
