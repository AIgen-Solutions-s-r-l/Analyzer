namespace AnalyzerCore.Infrastructure.RateLimiting;

/// <summary>
/// Rate limiter for WebSocket/SignalR connections.
/// </summary>
public interface IWebSocketRateLimiter
{
    /// <summary>
    /// Checks if a new connection is allowed from the given IP address.
    /// </summary>
    bool AllowConnection(string ipAddress);

    /// <summary>
    /// Registers a new connection.
    /// </summary>
    void RegisterConnection(string connectionId, string ipAddress);

    /// <summary>
    /// Unregisters a connection.
    /// </summary>
    void UnregisterConnection(string connectionId);

    /// <summary>
    /// Checks if a message is allowed for the given connection.
    /// </summary>
    bool AllowMessage(string connectionId);

    /// <summary>
    /// Checks if a new subscription is allowed for the given connection.
    /// </summary>
    bool AllowSubscription(string connectionId);

    /// <summary>
    /// Registers a subscription for the given connection.
    /// </summary>
    void RegisterSubscription(string connectionId, string subscription);

    /// <summary>
    /// Unregisters a subscription for the given connection.
    /// </summary>
    void UnregisterSubscription(string connectionId, string subscription);

    /// <summary>
    /// Gets the current number of violations for a connection.
    /// </summary>
    int GetViolationCount(string connectionId);

    /// <summary>
    /// Checks if a connection should be disconnected due to repeated violations.
    /// </summary>
    bool ShouldDisconnect(string connectionId);

    /// <summary>
    /// Gets statistics for monitoring.
    /// </summary>
    WebSocketRateLimitStats GetStats();
}

/// <summary>
/// Statistics for WebSocket rate limiting.
/// </summary>
public sealed record WebSocketRateLimitStats
{
    public int ActiveConnections { get; init; }
    public int TotalMessagesThisWindow { get; init; }
    public int RateLimitedMessages { get; init; }
    public int DisconnectedClients { get; init; }
    public Dictionary<string, int> ConnectionsPerIp { get; init; } = new();
}
