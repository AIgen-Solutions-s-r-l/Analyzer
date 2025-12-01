using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Events;
using AnalyzerCore.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Application.EventHandlers;

/// <summary>
/// Handles the PoolCreatedDomainEvent.
/// Can be used for cache invalidation, notifications, analytics, etc.
/// </summary>
public sealed class PoolCreatedDomainEventHandler : IDomainEventHandler<PoolCreatedDomainEvent>
{
    private readonly ILogger<PoolCreatedDomainEventHandler> _logger;

    public PoolCreatedDomainEventHandler(ILogger<PoolCreatedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PoolCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Pool created: {PoolAddress} ({PoolType}) - Token0: {Token0}, Token1: {Token1}, Factory: {Factory}",
            notification.PoolAddress,
            notification.PoolType,
            notification.Token0Address,
            notification.Token1Address,
            notification.FactoryAddress);

        // Future integrations:
        // - Invalidate related caches (pools by token, all pools, etc.)
        // - Send notifications to external systems
        // - Update analytics/metrics
        // - Trigger initial reserve fetch

        return Task.CompletedTask;
    }
}
