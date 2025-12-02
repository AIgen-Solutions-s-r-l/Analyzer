using AnalyzerCore.Application.Abstractions.Caching;
using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Events;
using AnalyzerCore.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Application.EventHandlers;

/// <summary>
/// Handles the TokenInfoUpdatedDomainEvent.
/// Triggered when a placeholder token's information is updated.
/// </summary>
public sealed class TokenInfoUpdatedDomainEventHandler : IDomainEventHandler<TokenInfoUpdatedDomainEvent>
{
    private readonly ICacheService _cacheService;
    private readonly IRealtimeNotificationService _notificationService;
    private readonly ILogger<TokenInfoUpdatedDomainEventHandler> _logger;

    public TokenInfoUpdatedDomainEventHandler(
        ICacheService cacheService,
        IRealtimeNotificationService notificationService,
        ILogger<TokenInfoUpdatedDomainEventHandler> logger)
    {
        _cacheService = cacheService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(TokenInfoUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Token info updated: {Address} - Symbol: {OldSymbol} -> {NewSymbol}, Name: {OldName} -> {NewName}",
            notification.TokenAddress,
            notification.OldSymbol,
            notification.NewSymbol,
            notification.OldName,
            notification.NewName);

        // Invalidate token-related caches
        await InvalidateTokenCachesAsync(notification.TokenAddress, cancellationToken);

        // Notify connected clients about the token update
        await NotifyClientsAsync(notification, cancellationToken);
    }

    private async Task InvalidateTokenCachesAsync(string tokenAddress, CancellationToken cancellationToken)
    {
        try
        {
            // Invalidate token cache entries
            await _cacheService.RemoveAsync($"token:{tokenAddress}", cancellationToken);

            // Invalidate price-related caches for this token
            await _cacheService.RemoveByPrefixAsync($"price:{tokenAddress}", cancellationToken);

            // Invalidate liquidity caches for this token
            await _cacheService.RemoveByPrefixAsync($"liquidity:token:{tokenAddress}", cancellationToken);

            // Invalidate volume caches
            await _cacheService.RemoveByPrefixAsync($"volume:token:{tokenAddress}", cancellationToken);

            _logger.LogDebug("Invalidated caches for token {TokenAddress}", tokenAddress);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate caches for token {TokenAddress}", tokenAddress);
        }
    }

    private async Task NotifyClientsAsync(TokenInfoUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.NotifyTokenUpdatedAsync(
                notification.TokenAddress,
                notification.NewSymbol,
                notification.NewName,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify clients about token update for {TokenAddress}", notification.TokenAddress);
        }
    }
}
