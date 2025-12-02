using AnalyzerCore.Api.Hubs.Filters;
using AnalyzerCore.Infrastructure.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AnalyzerCore.Api.Tests.Hubs.Filters;

public class HubRateLimitFilterTests
{
    private readonly Mock<IWebSocketRateLimiter> _rateLimiterMock;
    private readonly HubRateLimitFilter _filter;

    public HubRateLimitFilterTests()
    {
        _rateLimiterMock = new Mock<IWebSocketRateLimiter>();
        _filter = new HubRateLimitFilter(
            _rateLimiterMock.Object,
            NullLogger<HubRateLimitFilter>.Instance);
    }

    #region InvokeMethodAsync Tests

    [Fact]
    public async Task InvokeMethodAsync_WhenMessageAllowed_ShouldCallNext()
    {
        // Arrange
        var connectionId = "test_connection";
        var invocationContext = CreateInvocationContext(connectionId, "SomeMethod");
        var nextCalled = false;

        _rateLimiterMock.Setup(x => x.AllowMessage(connectionId)).Returns(true);

        // Act
        await _filter.InvokeMethodAsync(
            invocationContext,
            ctx =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(null);
            });

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeMethodAsync_WhenMessageBlocked_ShouldThrowHubException()
    {
        // Arrange
        var connectionId = "test_connection";
        var invocationContext = CreateInvocationContext(connectionId, "SomeMethod");

        _rateLimiterMock.Setup(x => x.AllowMessage(connectionId)).Returns(false);
        _rateLimiterMock.Setup(x => x.ShouldDisconnect(connectionId)).Returns(false);

        // Act & Assert
        var act = () => _filter.InvokeMethodAsync(
            invocationContext,
            ctx => ValueTask.FromResult<object?>(null));

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*Rate limit*");
    }

    [Fact]
    public async Task InvokeMethodAsync_WhenShouldDisconnect_ShouldAbortConnection()
    {
        // Arrange
        var connectionId = "test_connection";
        var hubContextMock = new Mock<HubCallerContext>();
        var aborted = false;

        hubContextMock.Setup(x => x.ConnectionId).Returns(connectionId);
        hubContextMock.Setup(x => x.Abort()).Callback(() => aborted = true);

        var invocationContext = CreateInvocationContext(hubContextMock.Object, "SomeMethod");

        _rateLimiterMock.Setup(x => x.AllowMessage(connectionId)).Returns(false);
        _rateLimiterMock.Setup(x => x.ShouldDisconnect(connectionId)).Returns(true);

        // Act
        await _filter.InvokeMethodAsync(
            invocationContext,
            ctx => ValueTask.FromResult<object?>(null));

        // Assert
        aborted.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeMethodAsync_WhenSubscriptionMethod_ShouldCheckSubscriptionLimit()
    {
        // Arrange
        var connectionId = "test_connection";
        var invocationContext = CreateInvocationContext(
            connectionId,
            "SubscribeToPool",
            "0x1234567890123456789012345678901234567890");

        _rateLimiterMock.Setup(x => x.AllowMessage(connectionId)).Returns(true);
        _rateLimiterMock.Setup(x => x.AllowSubscription(connectionId)).Returns(true);

        // Act
        await _filter.InvokeMethodAsync(
            invocationContext,
            ctx => ValueTask.FromResult<object?>(null));

        // Assert
        _rateLimiterMock.Verify(x => x.AllowSubscription(connectionId), Times.Once);
        _rateLimiterMock.Verify(x => x.RegisterSubscription(
            connectionId,
            "pool:0x1234567890123456789012345678901234567890"), Times.Once);
    }

    [Fact]
    public async Task InvokeMethodAsync_WhenSubscriptionLimitReached_ShouldThrowHubException()
    {
        // Arrange
        var connectionId = "test_connection";
        var invocationContext = CreateInvocationContext(
            connectionId,
            "SubscribeToToken",
            "0x1234567890123456789012345678901234567890");

        _rateLimiterMock.Setup(x => x.AllowMessage(connectionId)).Returns(true);
        _rateLimiterMock.Setup(x => x.AllowSubscription(connectionId)).Returns(false);

        // Act & Assert
        var act = () => _filter.InvokeMethodAsync(
            invocationContext,
            ctx => ValueTask.FromResult<object?>(null));

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*Maximum subscriptions*");
    }

    [Fact]
    public async Task InvokeMethodAsync_WhenUnsubscribeMethod_ShouldUnregisterSubscription()
    {
        // Arrange
        var connectionId = "test_connection";
        var poolAddress = "0x1234567890123456789012345678901234567890";
        var invocationContext = CreateInvocationContext(
            connectionId,
            "UnsubscribeFromPool",
            poolAddress);

        _rateLimiterMock.Setup(x => x.AllowMessage(connectionId)).Returns(true);

        // Act
        await _filter.InvokeMethodAsync(
            invocationContext,
            ctx => ValueTask.FromResult<object?>(null));

        // Assert
        _rateLimiterMock.Verify(x => x.UnregisterSubscription(
            connectionId,
            $"pool:{poolAddress}"), Times.Once);
    }

    #endregion

    #region OnConnectedAsync Tests

    [Fact]
    public async Task OnConnectedAsync_WhenConnectionAllowed_ShouldRegisterConnection()
    {
        // Arrange
        var connectionId = "test_connection";
        var ipAddress = "192.168.1.1";
        var lifetimeContext = CreateLifetimeContext(connectionId, ipAddress);

        _rateLimiterMock.Setup(x => x.AllowConnection(ipAddress)).Returns(true);

        // Act
        await _filter.OnConnectedAsync(lifetimeContext, ctx => Task.CompletedTask);

        // Assert
        _rateLimiterMock.Verify(x => x.RegisterConnection(connectionId, ipAddress), Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_WhenConnectionNotAllowed_ShouldAbort()
    {
        // Arrange
        var connectionId = "test_connection";
        var ipAddress = "192.168.1.1";
        var aborted = false;

        var hubContextMock = new Mock<HubCallerContext>();
        hubContextMock.Setup(x => x.ConnectionId).Returns(connectionId);
        hubContextMock.Setup(x => x.Abort()).Callback(() => aborted = true);

        var httpContextMock = new Mock<HttpContext>();
        var connectionInfoMock = new Mock<ConnectionInfo>();
        connectionInfoMock.Setup(x => x.RemoteIpAddress)
            .Returns(System.Net.IPAddress.Parse(ipAddress));
        httpContextMock.Setup(x => x.Connection).Returns(connectionInfoMock.Object);
        hubContextMock.Setup(x => x.GetHttpContext()).Returns(httpContextMock.Object);

        var lifetimeContext = new HubLifetimeContext(
            hubContextMock.Object,
            Mock.Of<IServiceProvider>(),
            Mock.Of<Hub>());

        _rateLimiterMock.Setup(x => x.AllowConnection(ipAddress)).Returns(false);

        // Act
        await _filter.OnConnectedAsync(lifetimeContext, ctx => Task.CompletedTask);

        // Assert
        aborted.Should().BeTrue();
        _rateLimiterMock.Verify(x => x.RegisterConnection(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region OnDisconnectedAsync Tests

    [Fact]
    public async Task OnDisconnectedAsync_ShouldUnregisterConnection()
    {
        // Arrange
        var connectionId = "test_connection";
        var lifetimeContext = CreateLifetimeContext(connectionId, "192.168.1.1");

        // Act
        await _filter.OnDisconnectedAsync(
            lifetimeContext,
            null,
            (ctx, ex) => Task.CompletedTask);

        // Assert
        _rateLimiterMock.Verify(x => x.UnregisterConnection(connectionId), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static HubInvocationContext CreateInvocationContext(
        string connectionId,
        string methodName,
        params object[] args)
    {
        var hubContextMock = new Mock<HubCallerContext>();
        hubContextMock.Setup(x => x.ConnectionId).Returns(connectionId);

        return CreateInvocationContext(hubContextMock.Object, methodName, args);
    }

    private static HubInvocationContext CreateInvocationContext(
        HubCallerContext hubContext,
        string methodName,
        params object[] args)
    {
        return new HubInvocationContext(
            hubContext,
            Mock.Of<IServiceProvider>(),
            Mock.Of<Hub>(),
            typeof(Hub).GetMethod("ToString")!, // Placeholder method info
            args.ToList())
        {
            // Use reflection to set HubMethodName since it's read-only
        };
    }

    private static HubLifetimeContext CreateLifetimeContext(string connectionId, string ipAddress)
    {
        var hubContextMock = new Mock<HubCallerContext>();
        hubContextMock.Setup(x => x.ConnectionId).Returns(connectionId);

        var httpContextMock = new Mock<HttpContext>();
        var connectionInfoMock = new Mock<ConnectionInfo>();
        connectionInfoMock.Setup(x => x.RemoteIpAddress)
            .Returns(System.Net.IPAddress.Parse(ipAddress));
        httpContextMock.Setup(x => x.Connection).Returns(connectionInfoMock.Object);
        hubContextMock.Setup(x => x.GetHttpContext()).Returns(httpContextMock.Object);

        return new HubLifetimeContext(
            hubContextMock.Object,
            Mock.Of<IServiceProvider>(),
            Mock.Of<Hub>());
    }

    #endregion
}
