using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Application.EventHandlers;

/// <summary>
/// Handles the TokenInfoUpdatedDomainEvent.
/// Triggered when a placeholder token's information is updated.
/// </summary>
public sealed class TokenInfoUpdatedDomainEventHandler : IDomainEventHandler<TokenInfoUpdatedDomainEvent>
{
    private readonly ILogger<TokenInfoUpdatedDomainEventHandler> _logger;

    public TokenInfoUpdatedDomainEventHandler(ILogger<TokenInfoUpdatedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TokenInfoUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Token info updated: {Address} - Symbol: {OldSymbol} -> {NewSymbol}, Name: {OldName} -> {NewName}",
            notification.TokenAddress,
            notification.OldSymbol,
            notification.NewSymbol,
            notification.OldName,
            notification.NewName);

        // Future integrations:
        // - Invalidate token cache
        // - Update dependent entities (pools referencing this token)
        // - Notify external systems of the update

        return Task.CompletedTask;
    }
}
