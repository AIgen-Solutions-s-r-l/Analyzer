using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AnalyzerCore.Api.Hubs;

/// <summary>
/// SignalR hub for real-time blockchain data updates.
/// </summary>
[Authorize(Policy = "RequireReadOnly")]
public class BlockchainHub : Hub
{
    private readonly ILogger<BlockchainHub> _logger;

    public BlockchainHub(ILogger<BlockchainHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "Client connected: {ConnectionId}",
            Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(
                exception,
                "Client disconnected with error: {ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation(
                "Client disconnected: {ConnectionId}",
                Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to pool updates for a specific pool.
    /// </summary>
    /// <param name="poolAddress">The pool address to subscribe to.</param>
    public async Task SubscribeToPool(string poolAddress)
    {
        var normalizedAddress = poolAddress.ToLowerInvariant();
        await Groups.AddToGroupAsync(Context.ConnectionId, $"pool:{normalizedAddress}");

        _logger.LogDebug(
            "Client {ConnectionId} subscribed to pool {PoolAddress}",
            Context.ConnectionId,
            normalizedAddress);
    }

    /// <summary>
    /// Unsubscribe from pool updates.
    /// </summary>
    /// <param name="poolAddress">The pool address to unsubscribe from.</param>
    public async Task UnsubscribeFromPool(string poolAddress)
    {
        var normalizedAddress = poolAddress.ToLowerInvariant();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"pool:{normalizedAddress}");

        _logger.LogDebug(
            "Client {ConnectionId} unsubscribed from pool {PoolAddress}",
            Context.ConnectionId,
            normalizedAddress);
    }

    /// <summary>
    /// Subscribe to token price updates.
    /// </summary>
    /// <param name="tokenAddress">The token address to subscribe to.</param>
    public async Task SubscribeToToken(string tokenAddress)
    {
        var normalizedAddress = tokenAddress.ToLowerInvariant();
        await Groups.AddToGroupAsync(Context.ConnectionId, $"token:{normalizedAddress}");

        _logger.LogDebug(
            "Client {ConnectionId} subscribed to token {TokenAddress}",
            Context.ConnectionId,
            normalizedAddress);
    }

    /// <summary>
    /// Unsubscribe from token price updates.
    /// </summary>
    /// <param name="tokenAddress">The token address to unsubscribe from.</param>
    public async Task UnsubscribeFromToken(string tokenAddress)
    {
        var normalizedAddress = tokenAddress.ToLowerInvariant();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"token:{normalizedAddress}");

        _logger.LogDebug(
            "Client {ConnectionId} unsubscribed from token {TokenAddress}",
            Context.ConnectionId,
            normalizedAddress);
    }

    /// <summary>
    /// Subscribe to all new block events.
    /// </summary>
    public async Task SubscribeToBlocks()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "blocks");

        _logger.LogDebug(
            "Client {ConnectionId} subscribed to blocks",
            Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from block events.
    /// </summary>
    public async Task UnsubscribeFromBlocks()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "blocks");

        _logger.LogDebug(
            "Client {ConnectionId} unsubscribed from blocks",
            Context.ConnectionId);
    }

    /// <summary>
    /// Subscribe to arbitrage opportunity alerts.
    /// </summary>
    /// <param name="minProfitUsd">Minimum profit threshold (optional).</param>
    public async Task SubscribeToArbitrage(decimal? minProfitUsd = null)
    {
        var group = minProfitUsd.HasValue
            ? $"arbitrage:min{minProfitUsd.Value:F0}"
            : "arbitrage:all";

        await Groups.AddToGroupAsync(Context.ConnectionId, group);

        _logger.LogDebug(
            "Client {ConnectionId} subscribed to arbitrage (min: ${MinProfit})",
            Context.ConnectionId,
            minProfitUsd);
    }

    /// <summary>
    /// Unsubscribe from arbitrage alerts.
    /// </summary>
    public async Task UnsubscribeFromArbitrage()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "arbitrage:all");

        _logger.LogDebug(
            "Client {ConnectionId} unsubscribed from arbitrage",
            Context.ConnectionId);
    }

    /// <summary>
    /// Subscribe to new token discoveries.
    /// </summary>
    public async Task SubscribeToNewTokens()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "new-tokens");

        _logger.LogDebug(
            "Client {ConnectionId} subscribed to new tokens",
            Context.ConnectionId);
    }

    /// <summary>
    /// Subscribe to new pool discoveries.
    /// </summary>
    public async Task SubscribeToNewPools()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "new-pools");

        _logger.LogDebug(
            "Client {ConnectionId} subscribed to new pools",
            Context.ConnectionId);
    }
}
