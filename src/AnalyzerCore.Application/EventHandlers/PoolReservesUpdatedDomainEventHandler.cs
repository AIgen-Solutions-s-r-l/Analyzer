using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Application.EventHandlers;

/// <summary>
/// Handles the PoolReservesUpdatedDomainEvent.
/// Critical for monitoring liquidity changes and triggering dependent calculations.
/// </summary>
public sealed class PoolReservesUpdatedDomainEventHandler : IDomainEventHandler<PoolReservesUpdatedDomainEvent>
{
    private readonly ILogger<PoolReservesUpdatedDomainEventHandler> _logger;

    public PoolReservesUpdatedDomainEventHandler(ILogger<PoolReservesUpdatedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PoolReservesUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var reserve0Change = notification.NewReserve0 - notification.PreviousReserve0;
        var reserve1Change = notification.NewReserve1 - notification.PreviousReserve1;

        _logger.LogInformation(
            "Pool reserves updated: {PoolAddress} - Reserve0: {PrevR0} -> {NewR0} ({ChangeR0:+0.00;-0.00}), Reserve1: {PrevR1} -> {NewR1} ({ChangeR1:+0.00;-0.00})",
            notification.PoolAddress,
            notification.PreviousReserve0,
            notification.NewReserve0,
            reserve0Change,
            notification.PreviousReserve1,
            notification.NewReserve1,
            reserve1Change);

        // Future integrations:
        // - Invalidate pool cache
        // - Recalculate price impact metrics
        // - Update arbitrage detection algorithms
        // - Trigger price oracle updates
        // - Detect significant liquidity changes (potential rug pulls, large swaps)

        return Task.CompletedTask;
    }
}
