using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Infrastructure.Configuration;

/// <summary>
/// Configuration options for WebSocket/SignalR rate limiting.
/// </summary>
public sealed class WebSocketRateLimitOptions
{
    public const string SectionName = "WebSocketRateLimit";

    /// <summary>
    /// Whether WebSocket rate limiting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum messages per connection per window.
    /// </summary>
    [Range(1, 10000)]
    public int MaxMessagesPerWindow { get; set; } = 100;

    /// <summary>
    /// Window duration in seconds.
    /// </summary>
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum connections per IP address.
    /// </summary>
    [Range(1, 100)]
    public int MaxConnectionsPerIp { get; set; } = 5;

    /// <summary>
    /// Maximum subscriptions per connection.
    /// </summary>
    [Range(1, 1000)]
    public int MaxSubscriptionsPerConnection { get; set; } = 50;

    /// <summary>
    /// Cooldown in seconds after being rate limited before messages are accepted again.
    /// </summary>
    [Range(1, 300)]
    public int CooldownSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to disconnect clients that exceed rate limits repeatedly.
    /// </summary>
    public bool DisconnectOnRepeatedViolations { get; set; } = true;

    /// <summary>
    /// Number of violations before disconnecting (if DisconnectOnRepeatedViolations is true).
    /// </summary>
    [Range(1, 100)]
    public int ViolationsBeforeDisconnect { get; set; } = 3;
}
