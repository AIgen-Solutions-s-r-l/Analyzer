using AnalyzerCore.Infrastructure.Configuration;
using AnalyzerCore.Infrastructure.RateLimiting;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AnalyzerCore.Infrastructure.Tests.RateLimiting;

public class WebSocketRateLimiterTests
{
    private readonly WebSocketRateLimitOptions _options;
    private readonly WebSocketRateLimiter _rateLimiter;

    public WebSocketRateLimiterTests()
    {
        _options = new WebSocketRateLimitOptions
        {
            Enabled = true,
            MaxMessagesPerWindow = 10,
            WindowSeconds = 60,
            MaxConnectionsPerIp = 3,
            MaxSubscriptionsPerConnection = 5,
            CooldownSeconds = 30,
            DisconnectOnRepeatedViolations = true,
            ViolationsBeforeDisconnect = 3
        };

        _rateLimiter = new WebSocketRateLimiter(
            Options.Create(_options),
            NullLogger<WebSocketRateLimiter>.Instance);
    }

    #region Connection Tests

    [Fact]
    public void AllowConnection_WhenUnderLimit_ShouldReturnTrue()
    {
        // Act
        var result = _rateLimiter.AllowConnection("192.168.1.1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AllowConnection_WhenAtLimit_ShouldReturnFalse()
    {
        // Arrange
        var ip = "192.168.1.1";
        for (int i = 0; i < _options.MaxConnectionsPerIp; i++)
        {
            _rateLimiter.RegisterConnection($"conn_{i}", ip);
        }

        // Act
        var result = _rateLimiter.AllowConnection(ip);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RegisterConnection_ShouldTrackConnection()
    {
        // Act
        _rateLimiter.RegisterConnection("conn_1", "192.168.1.1");
        var stats = _rateLimiter.GetStats();

        // Assert
        stats.ActiveConnections.Should().Be(1);
        stats.ConnectionsPerIp.Should().ContainKey("192.168.1.1");
        stats.ConnectionsPerIp["192.168.1.1"].Should().Be(1);
    }

    [Fact]
    public void UnregisterConnection_ShouldRemoveConnection()
    {
        // Arrange
        _rateLimiter.RegisterConnection("conn_1", "192.168.1.1");

        // Act
        _rateLimiter.UnregisterConnection("conn_1");
        var stats = _rateLimiter.GetStats();

        // Assert
        stats.ActiveConnections.Should().Be(0);
    }

    [Fact]
    public void UnregisterConnection_ShouldAllowNewConnectionFromSameIp()
    {
        // Arrange
        var ip = "192.168.1.1";
        for (int i = 0; i < _options.MaxConnectionsPerIp; i++)
        {
            _rateLimiter.RegisterConnection($"conn_{i}", ip);
        }

        // Verify at limit
        _rateLimiter.AllowConnection(ip).Should().BeFalse();

        // Act
        _rateLimiter.UnregisterConnection("conn_0");

        // Assert
        _rateLimiter.AllowConnection(ip).Should().BeTrue();
    }

    #endregion

    #region Message Rate Limiting Tests

    [Fact]
    public void AllowMessage_WhenUnderLimit_ShouldReturnTrue()
    {
        // Arrange
        _rateLimiter.RegisterConnection("conn_1", "192.168.1.1");

        // Act
        var result = _rateLimiter.AllowMessage("conn_1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AllowMessage_WhenConnectionNotRegistered_ShouldReturnFalse()
    {
        // Act
        var result = _rateLimiter.AllowMessage("unknown_conn");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AllowMessage_WhenAtLimit_ShouldReturnFalse()
    {
        // Arrange
        _rateLimiter.RegisterConnection("conn_1", "192.168.1.1");

        // Send messages up to limit
        for (int i = 0; i < _options.MaxMessagesPerWindow; i++)
        {
            _rateLimiter.AllowMessage("conn_1").Should().BeTrue();
        }

        // Act - next message should be blocked
        var result = _rateLimiter.AllowMessage("conn_1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AllowMessage_WhenExceedingLimit_ShouldIncrementViolations()
    {
        // Arrange
        _rateLimiter.RegisterConnection("conn_1", "192.168.1.1");

        // Exceed limit
        for (int i = 0; i <= _options.MaxMessagesPerWindow; i++)
        {
            _rateLimiter.AllowMessage("conn_1");
        }

        // Act
        var violations = _rateLimiter.GetViolationCount("conn_1");

        // Assert
        violations.Should().Be(1);
    }

    #endregion

    #region Subscription Tests

    [Fact]
    public void AllowSubscription_WhenUnderLimit_ShouldReturnTrue()
    {
        // Arrange
        _rateLimiter.RegisterConnection("conn_1", "192.168.1.1");

        // Act
        var result = _rateLimiter.AllowSubscription("conn_1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AllowSubscription_WhenAtLimit_ShouldReturnFalse()
    {
        // Arrange
        _rateLimiter.RegisterConnection("conn_1", "192.168.1.1");

        // Register max subscriptions
        for (int i = 0; i < _options.MaxSubscriptionsPerConnection; i++)
        {
            _rateLimiter.RegisterSubscription("conn_1", $"sub_{i}");
        }

        // Act
        var result = _rateLimiter.AllowSubscription("conn_1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void UnregisterSubscription_ShouldAllowNewSubscription()
    {
        // Arrange
        _rateLimiter.RegisterConnection("conn_1", "192.168.1.1");

        for (int i = 0; i < _options.MaxSubscriptionsPerConnection; i++)
        {
            _rateLimiter.RegisterSubscription("conn_1", $"sub_{i}");
        }

        // Verify at limit
        _rateLimiter.AllowSubscription("conn_1").Should().BeFalse();

        // Act
        _rateLimiter.UnregisterSubscription("conn_1", "sub_0");

        // Assert
        _rateLimiter.AllowSubscription("conn_1").Should().BeTrue();
    }

    #endregion

    #region Disconnect Tests

    [Fact]
    public void ShouldDisconnect_WhenUnderViolationThreshold_ShouldReturnFalse()
    {
        // Arrange
        _rateLimiter.RegisterConnection("conn_1", "192.168.1.1");

        // Act
        var result = _rateLimiter.ShouldDisconnect("conn_1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldDisconnect_WhenAtViolationThreshold_ShouldReturnTrue()
    {
        // Arrange
        _rateLimiter.RegisterConnection("conn_1", "192.168.1.1");

        // Generate violations by exceeding message limit multiple times
        for (int violation = 0; violation < _options.ViolationsBeforeDisconnect; violation++)
        {
            // Fill up the window
            for (int i = 0; i <= _options.MaxMessagesPerWindow; i++)
            {
                _rateLimiter.AllowMessage("conn_1");
            }
        }

        // Act
        var result = _rateLimiter.ShouldDisconnect("conn_1");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Disabled Rate Limiter Tests

    [Fact]
    public void AllowMessage_WhenDisabled_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var disabledOptions = new WebSocketRateLimitOptions { Enabled = false };
        var rateLimiter = new WebSocketRateLimiter(
            Options.Create(disabledOptions),
            NullLogger<WebSocketRateLimiter>.Instance);

        // Act - should allow any message even without registration
        var result = rateLimiter.AllowMessage("any_connection");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AllowConnection_WhenDisabled_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var disabledOptions = new WebSocketRateLimitOptions { Enabled = false };
        var rateLimiter = new WebSocketRateLimiter(
            Options.Create(disabledOptions),
            NullLogger<WebSocketRateLimiter>.Instance);

        // Act - should allow unlimited connections from same IP
        for (int i = 0; i < 100; i++)
        {
            rateLimiter.AllowConnection("192.168.1.1").Should().BeTrue();
        }
    }

    #endregion

    #region Stats Tests

    [Fact]
    public void GetStats_ShouldReturnAccurateStats()
    {
        // Arrange
        _rateLimiter.RegisterConnection("conn_1", "192.168.1.1");
        _rateLimiter.RegisterConnection("conn_2", "192.168.1.1");
        _rateLimiter.RegisterConnection("conn_3", "192.168.1.2");

        _rateLimiter.AllowMessage("conn_1");
        _rateLimiter.AllowMessage("conn_1");
        _rateLimiter.AllowMessage("conn_2");

        // Act
        var stats = _rateLimiter.GetStats();

        // Assert
        stats.ActiveConnections.Should().Be(3);
        stats.TotalMessagesThisWindow.Should().Be(3);
        stats.ConnectionsPerIp.Should().HaveCount(2);
        stats.ConnectionsPerIp["192.168.1.1"].Should().Be(2);
        stats.ConnectionsPerIp["192.168.1.2"].Should().Be(1);
    }

    #endregion
}
