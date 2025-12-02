using System.Collections.Concurrent;
using AnalyzerCore.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalyzerCore.Infrastructure.RateLimiting;

/// <summary>
/// Implementation of WebSocket rate limiting using sliding windows.
/// </summary>
public sealed class WebSocketRateLimiter : IWebSocketRateLimiter
{
    private readonly WebSocketRateLimitOptions _options;
    private readonly ILogger<WebSocketRateLimiter> _logger;

    private readonly ConcurrentDictionary<string, ConnectionState> _connections = new();
    private readonly ConcurrentDictionary<string, int> _ipConnectionCounts = new();

    private int _totalRateLimitedMessages;
    private int _totalDisconnectedClients;

    public WebSocketRateLimiter(
        IOptions<WebSocketRateLimitOptions> options,
        ILogger<WebSocketRateLimiter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool AllowConnection(string ipAddress)
    {
        if (!_options.Enabled)
        {
            return true;
        }

        var currentCount = _ipConnectionCounts.GetOrAdd(ipAddress, 0);
        if (currentCount >= _options.MaxConnectionsPerIp)
        {
            _logger.LogWarning(
                "Connection rejected: IP {IpAddress} has reached max connections ({Max})",
                ipAddress,
                _options.MaxConnectionsPerIp);
            return false;
        }

        return true;
    }

    public void RegisterConnection(string connectionId, string ipAddress)
    {
        var state = new ConnectionState
        {
            ConnectionId = connectionId,
            IpAddress = ipAddress,
            ConnectedAt = DateTime.UtcNow
        };

        _connections[connectionId] = state;
        _ipConnectionCounts.AddOrUpdate(ipAddress, 1, (_, count) => count + 1);

        _logger.LogDebug(
            "Connection registered: {ConnectionId} from {IpAddress}",
            connectionId,
            ipAddress);
    }

    public void UnregisterConnection(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var state))
        {
            _ipConnectionCounts.AddOrUpdate(
                state.IpAddress,
                0,
                (_, count) => Math.Max(0, count - 1));

            _logger.LogDebug(
                "Connection unregistered: {ConnectionId}",
                connectionId);
        }
    }

    public bool AllowMessage(string connectionId)
    {
        if (!_options.Enabled)
        {
            return true;
        }

        if (!_connections.TryGetValue(connectionId, out var state))
        {
            return false;
        }

        // Check if in cooldown
        if (state.CooldownUntil.HasValue && DateTime.UtcNow < state.CooldownUntil.Value)
        {
            _logger.LogDebug(
                "Message blocked: {ConnectionId} is in cooldown until {CooldownUntil}",
                connectionId,
                state.CooldownUntil.Value);
            return false;
        }

        // Cleanup old messages
        CleanupOldMessages(state);

        // Check rate limit
        if (state.MessageTimestamps.Count >= _options.MaxMessagesPerWindow)
        {
            state.ViolationCount++;
            Interlocked.Increment(ref _totalRateLimitedMessages);

            if (_options.DisconnectOnRepeatedViolations &&
                state.ViolationCount >= _options.ViolationsBeforeDisconnect)
            {
                _logger.LogWarning(
                    "Connection {ConnectionId} exceeded violation threshold ({Violations})",
                    connectionId,
                    state.ViolationCount);
            }
            else
            {
                // Apply cooldown
                state.CooldownUntil = DateTime.UtcNow.AddSeconds(_options.CooldownSeconds);
                _logger.LogWarning(
                    "Message rate limited: {ConnectionId} exceeded {Max} messages per {Window}s window. Cooldown until {CooldownUntil}",
                    connectionId,
                    _options.MaxMessagesPerWindow,
                    _options.WindowSeconds,
                    state.CooldownUntil.Value);
            }

            return false;
        }

        // Record the message
        state.MessageTimestamps.Enqueue(DateTime.UtcNow);
        return true;
    }

    public bool AllowSubscription(string connectionId)
    {
        if (!_options.Enabled)
        {
            return true;
        }

        if (!_connections.TryGetValue(connectionId, out var state))
        {
            return false;
        }

        if (state.Subscriptions.Count >= _options.MaxSubscriptionsPerConnection)
        {
            _logger.LogWarning(
                "Subscription rejected: {ConnectionId} has reached max subscriptions ({Max})",
                connectionId,
                _options.MaxSubscriptionsPerConnection);
            return false;
        }

        return true;
    }

    public void RegisterSubscription(string connectionId, string subscription)
    {
        if (_connections.TryGetValue(connectionId, out var state))
        {
            state.Subscriptions.Add(subscription);
        }
    }

    public void UnregisterSubscription(string connectionId, string subscription)
    {
        if (_connections.TryGetValue(connectionId, out var state))
        {
            state.Subscriptions.Remove(subscription);
        }
    }

    public int GetViolationCount(string connectionId)
    {
        if (_connections.TryGetValue(connectionId, out var state))
        {
            return state.ViolationCount;
        }
        return 0;
    }

    public bool ShouldDisconnect(string connectionId)
    {
        if (!_options.DisconnectOnRepeatedViolations)
        {
            return false;
        }

        if (_connections.TryGetValue(connectionId, out var state))
        {
            if (state.ViolationCount >= _options.ViolationsBeforeDisconnect)
            {
                Interlocked.Increment(ref _totalDisconnectedClients);
                return true;
            }
        }
        return false;
    }

    public WebSocketRateLimitStats GetStats()
    {
        var totalMessages = 0;
        foreach (var state in _connections.Values)
        {
            CleanupOldMessages(state);
            totalMessages += state.MessageTimestamps.Count;
        }

        return new WebSocketRateLimitStats
        {
            ActiveConnections = _connections.Count,
            TotalMessagesThisWindow = totalMessages,
            RateLimitedMessages = _totalRateLimitedMessages,
            DisconnectedClients = _totalDisconnectedClients,
            ConnectionsPerIp = _ipConnectionCounts
                .Where(kvp => kvp.Value > 0)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }

    private void CleanupOldMessages(ConnectionState state)
    {
        var windowStart = DateTime.UtcNow.AddSeconds(-_options.WindowSeconds);

        while (state.MessageTimestamps.TryPeek(out var timestamp) && timestamp < windowStart)
        {
            state.MessageTimestamps.TryDequeue(out _);
        }
    }

    private sealed class ConnectionState
    {
        public string ConnectionId { get; init; } = string.Empty;
        public string IpAddress { get; init; } = string.Empty;
        public DateTime ConnectedAt { get; init; }
        public ConcurrentQueue<DateTime> MessageTimestamps { get; } = new();
        public HashSet<string> Subscriptions { get; } = new();
        public int ViolationCount { get; set; }
        public DateTime? CooldownUntil { get; set; }
    }
}
