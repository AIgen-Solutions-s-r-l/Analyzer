using AnalyzerCore.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace AnalyzerCore.Api.Hubs.Filters;

/// <summary>
/// SignalR hub filter that applies rate limiting to hub method invocations.
/// </summary>
public class HubRateLimitFilter : IHubFilter
{
    private readonly IWebSocketRateLimiter _rateLimiter;
    private readonly ILogger<HubRateLimitFilter> _logger;

    public HubRateLimitFilter(
        IWebSocketRateLimiter rateLimiter,
        ILogger<HubRateLimitFilter> logger)
    {
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var connectionId = invocationContext.Context.ConnectionId;

        // Check if message is allowed
        if (!_rateLimiter.AllowMessage(connectionId))
        {
            // Check if we should disconnect
            if (_rateLimiter.ShouldDisconnect(connectionId))
            {
                _logger.LogWarning(
                    "Disconnecting {ConnectionId} due to repeated rate limit violations",
                    connectionId);

                invocationContext.Context.Abort();
                return null;
            }

            _logger.LogDebug(
                "Rate limited message from {ConnectionId} for method {Method}",
                connectionId,
                invocationContext.HubMethodName);

            throw new HubException("Rate limit exceeded. Please slow down.");
        }

        // Handle subscription methods
        if (IsSubscriptionMethod(invocationContext.HubMethodName))
        {
            if (!_rateLimiter.AllowSubscription(connectionId))
            {
                throw new HubException("Maximum subscriptions reached.");
            }

            var result = await next(invocationContext);

            // Register the subscription
            var subscriptionKey = GetSubscriptionKey(invocationContext);
            if (!string.IsNullOrEmpty(subscriptionKey))
            {
                _rateLimiter.RegisterSubscription(connectionId, subscriptionKey);
            }

            return result;
        }

        // Handle unsubscription methods
        if (IsUnsubscribeMethod(invocationContext.HubMethodName))
        {
            var result = await next(invocationContext);

            // Unregister the subscription
            var subscriptionKey = GetSubscriptionKey(invocationContext);
            if (!string.IsNullOrEmpty(subscriptionKey))
            {
                _rateLimiter.UnregisterSubscription(connectionId, subscriptionKey);
            }

            return result;
        }

        return await next(invocationContext);
    }

    public Task OnConnectedAsync(
        HubLifetimeContext context,
        Func<HubLifetimeContext, Task> next)
    {
        var httpContext = context.Context.GetHttpContext();
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

        if (!_rateLimiter.AllowConnection(ipAddress))
        {
            _logger.LogWarning(
                "Connection rejected for IP {IpAddress}: max connections exceeded",
                ipAddress);

            context.Context.Abort();
            return Task.CompletedTask;
        }

        _rateLimiter.RegisterConnection(context.Context.ConnectionId, ipAddress);

        return next(context);
    }

    public Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        _rateLimiter.UnregisterConnection(context.Context.ConnectionId);

        return next(context, exception);
    }

    private static bool IsSubscriptionMethod(string methodName)
    {
        return methodName.StartsWith("SubscribeTo", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnsubscribeMethod(string methodName)
    {
        return methodName.StartsWith("UnsubscribeFrom", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSubscriptionKey(HubInvocationContext context)
    {
        var methodName = context.HubMethodName;
        var args = context.HubMethodArguments;

        // Extract the subscription identifier based on method
        return methodName switch
        {
            "SubscribeToPool" or "UnsubscribeFromPool" when args.Count > 0 =>
                $"pool:{args[0]}",
            "SubscribeToToken" or "UnsubscribeFromToken" when args.Count > 0 =>
                $"token:{args[0]}",
            "SubscribeToBlocks" or "UnsubscribeFromBlocks" =>
                "blocks",
            "SubscribeToArbitrage" =>
                args.Count > 0 && args[0] != null
                    ? $"arbitrage:min{args[0]}"
                    : "arbitrage:all",
            "UnsubscribeFromArbitrage" =>
                "arbitrage:all",
            "SubscribeToNewTokens" =>
                "new-tokens",
            "SubscribeToNewPools" =>
                "new-pools",
            _ => string.Empty
        };
    }
}
