using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Application.EventHandlers;

/// <summary>
/// Handles the TokenCreatedDomainEvent.
/// Can be used for cache invalidation, notifications, analytics, etc.
/// </summary>
public sealed class TokenCreatedDomainEventHandler : IDomainEventHandler<TokenCreatedDomainEvent>
{
    private readonly ILogger<TokenCreatedDomainEventHandler> _logger;

    public TokenCreatedDomainEventHandler(ILogger<TokenCreatedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TokenCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Token created: {Symbol} ({Address}) on chain {ChainId}",
            notification.Symbol,
            notification.TokenAddress,
            notification.ChainId);

        // Future integrations:
        // - Invalidate related caches
        // - Send notifications to external systems
        // - Update analytics/metrics
        // - Trigger price discovery

        return Task.CompletedTask;
    }
}
