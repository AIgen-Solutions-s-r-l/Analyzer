using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AnalyzerCore.Api.Hubs;

/// <summary>
/// SignalR hub for real-time blockchain data updates.
/// </summary>
/// <remarks>
/// The BlockchainHub provides real-time WebSocket connectivity for streaming
/// blockchain events, price updates, and arbitrage alerts to connected clients.
///
/// ## Connection URL
/// ```
/// wss://api.example.com/hubs/blockchain
/// ```
///
/// ## Authentication
/// Include your API key as a query parameter or header:
/// ```javascript
/// const connection = new signalR.HubConnectionBuilder()
///     .withUrl("/hubs/blockchain", {
///         accessTokenFactory: () => "your-api-key"
///     })
///     .build();
/// ```
///
/// ## Available Subscriptions
/// - **Pools**: Real-time reserve updates for specific liquidity pools
/// - **Tokens**: Price updates for specific tokens
/// - **Blocks**: New block notifications
/// - **Arbitrage**: Arbitrage opportunity alerts with optional profit filter
/// - **New Tokens**: Notifications when new tokens are discovered
/// - **New Pools**: Notifications when new pools are created
///
/// ## Client Methods (call these)
/// - `SubscribeToPool(poolAddress)` - Subscribe to pool updates
/// - `UnsubscribeFromPool(poolAddress)` - Unsubscribe from pool updates
/// - `SubscribeToToken(tokenAddress)` - Subscribe to token price updates
/// - `UnsubscribeFromToken(tokenAddress)` - Unsubscribe from token updates
/// - `SubscribeToBlocks()` - Subscribe to new block events
/// - `UnsubscribeFromBlocks()` - Unsubscribe from block events
/// - `SubscribeToArbitrage(minProfitUsd?)` - Subscribe to arbitrage alerts
/// - `UnsubscribeFromArbitrage()` - Unsubscribe from arbitrage alerts
/// - `SubscribeToNewTokens()` - Subscribe to new token discoveries
/// - `SubscribeToNewPools()` - Subscribe to new pool discoveries
///
/// ## Server Events (listen to these)
/// - `ReceivePoolUpdate` - Pool reserve changes
/// - `ReceivePriceUpdate` - Token price changes
/// - `ReceiveBlockUpdate` - New block mined
/// - `ReceiveArbitrageAlert` - Arbitrage opportunity detected
/// - `ReceiveNewToken` - New token discovered
/// - `ReceiveNewPool` - New pool created
/// - `ReceiveSignificantPriceChange` - Large price movement detected
///
/// ## Rate Limits
/// - Maximum 100 active subscriptions per connection
/// - Message rate: 10 messages/second outbound
/// - Connection limit: 5 concurrent connections per API key
///
/// ## Example JavaScript Client
/// ```javascript
/// // Connect
/// const connection = new signalR.HubConnectionBuilder()
///     .withUrl("/hubs/blockchain", {
///         accessTokenFactory: () => apiKey
///     })
///     .withAutomaticReconnect()
///     .build();
///
/// // Listen for price updates
/// connection.on("ReceivePriceUpdate", (data) =&gt; {
///     console.log(`${data.tokenAddress}: $${data.priceUsd}`);
/// });
///
/// // Subscribe to a token
/// await connection.start();
/// await connection.invoke("SubscribeToToken", "0xc02aaa39...");
/// ```
/// </remarks>
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
    /// <remarks>
    /// Receive real-time notifications when pool reserves change.
    /// You'll receive `ReceivePoolUpdate` events with reserve0, reserve1, and timestamp.
    ///
    /// ### Example
    /// ```javascript
    /// await connection.invoke("SubscribeToPool", "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852");
    /// ```
    /// </remarks>
    /// <param name="poolAddress">The liquidity pool contract address (0x-prefixed).</param>
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
    /// <remarks>
    /// Receive real-time price updates for a specific token.
    /// You'll receive `ReceivePriceUpdate` events with price, priceUsd, and liquidity data.
    ///
    /// ### Example
    /// ```javascript
    /// await connection.invoke("SubscribeToToken", "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2");
    /// ```
    /// </remarks>
    /// <param name="tokenAddress">The ERC-20 token contract address (0x-prefixed).</param>
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
    /// <remarks>
    /// Receive notifications when new blocks are mined on the monitored chain.
    /// You'll receive `ReceiveBlockUpdate` events with blockNumber, blockHash, and timestamp.
    ///
    /// ### Example
    /// ```javascript
    /// await connection.invoke("SubscribeToBlocks");
    /// connection.on("ReceiveBlockUpdate", (block) =&gt; {
    ///     console.log(`New block: ${block.blockNumber}`);
    /// });
    /// ```
    /// </remarks>
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
    /// <remarks>
    /// Receive real-time notifications when arbitrage opportunities are detected.
    /// You'll receive `ReceiveArbitrageAlert` events with profit details and confidence scores.
    ///
    /// ### Example
    /// ```javascript
    /// // Subscribe to opportunities with at least $50 profit
    /// await connection.invoke("SubscribeToArbitrage", 50);
    ///
    /// connection.on("ReceiveArbitrageAlert", (arb) =&gt; {
    ///     console.log(`${arb.tokenSymbol}: $${arb.netProfitUsd} profit`);
    /// });
    /// ```
    ///
    /// ### Filtering
    /// Specify `minProfitUsd` to only receive alerts above your threshold.
    /// Omit for all opportunities.
    /// </remarks>
    /// <param name="minProfitUsd">Minimum net profit in USD to trigger alerts (optional).</param>
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
    /// <remarks>
    /// Receive notifications when new ERC-20 tokens are discovered in monitored pools.
    /// You'll receive `ReceiveNewToken` events with token address, symbol, name, and decimals.
    ///
    /// ### Example
    /// ```javascript
    /// await connection.invoke("SubscribeToNewTokens");
    /// connection.on("ReceiveNewToken", (token) =&gt; {
    ///     console.log(`New token: ${token.symbol} (${token.address})`);
    /// });
    /// ```
    /// </remarks>
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
    /// <remarks>
    /// Receive notifications when new liquidity pools are discovered on monitored DEXs.
    /// You'll receive `ReceiveNewPool` events with pool address, token pair, and initial reserves.
    ///
    /// ### Example
    /// ```javascript
    /// await connection.invoke("SubscribeToNewPools");
    /// connection.on("ReceiveNewPool", (pool) =&gt; {
    ///     console.log(`New pool: ${pool.address} (${pool.token0Address}/${pool.token1Address})`);
    /// });
    /// ```
    /// </remarks>
    public async Task SubscribeToNewPools()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "new-pools");

        _logger.LogDebug(
            "Client {ConnectionId} subscribed to new pools",
            Context.ConnectionId);
    }
}
