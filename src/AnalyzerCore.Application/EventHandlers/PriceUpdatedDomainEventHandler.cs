using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Events;
using AnalyzerCore.Infrastructure.RealTime;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Application.EventHandlers;

/// <summary>
/// Handles the PriceUpdatedEvent.
/// Broadcasts price updates to connected clients.
/// </summary>
public sealed class PriceUpdatedDomainEventHandler : IDomainEventHandler<PriceUpdatedEvent>
{
    private readonly ILogger<PriceUpdatedDomainEventHandler> _logger;
    private readonly IRealtimeNotificationService _notificationService;

    public PriceUpdatedDomainEventHandler(
        ILogger<PriceUpdatedDomainEventHandler> logger,
        IRealtimeNotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(PriceUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Price updated for {TokenSymbol} ({TokenAddress}): {OldPrice} -> {NewPrice} ({Change:+0.00;-0.00}%)",
            notification.TokenSymbol,
            notification.TokenAddress,
            notification.OldPrice,
            notification.NewPrice,
            notification.PriceChangePercent);

        // Broadcast to subscribed clients
        await _notificationService.BroadcastPriceUpdateAsync(new PriceUpdateMessage
        {
            TokenAddress = notification.TokenAddress,
            TokenSymbol = notification.TokenSymbol,
            QuoteTokenSymbol = notification.QuoteTokenSymbol,
            OldPrice = notification.OldPrice,
            NewPrice = notification.NewPrice,
            PriceChangePercent = notification.PriceChangePercent,
            PriceUsd = notification.PriceUsd,
            PoolAddress = notification.PoolAddress,
            Timestamp = notification.OccurredAt
        });
    }
}
