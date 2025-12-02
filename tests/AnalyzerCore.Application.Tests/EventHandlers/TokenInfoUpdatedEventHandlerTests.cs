using AnalyzerCore.Application.Abstractions.Caching;
using AnalyzerCore.Application.EventHandlers;
using AnalyzerCore.Domain.Events;
using AnalyzerCore.Domain.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.EventHandlers;

public class TokenInfoUpdatedEventHandlerTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IRealtimeNotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<TokenInfoUpdatedDomainEventHandler>> _loggerMock;
    private readonly TokenInfoUpdatedDomainEventHandler _handler;

    public TokenInfoUpdatedEventHandlerTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _notificationServiceMock = new Mock<IRealtimeNotificationService>();
        _loggerMock = new Mock<ILogger<TokenInfoUpdatedDomainEventHandler>>();

        _handler = new TokenInfoUpdatedDomainEventHandler(
            _cacheServiceMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldInvalidateTokenCache()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var domainEvent = new TokenInfoUpdatedDomainEvent(
            tokenAddress,
            "PLACEHOLDER",
            "WETH",
            "Placeholder Token",
            "Wrapped Ether");

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(
            c => c.RemoveAsync($"token:{tokenAddress}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldInvalidatePriceCaches()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var domainEvent = new TokenInfoUpdatedDomainEvent(
            tokenAddress,
            "OLD",
            "NEW",
            "Old Name",
            "New Name");

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(
            c => c.RemoveByPrefixAsync($"price:{tokenAddress}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldInvalidateLiquidityCaches()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var domainEvent = new TokenInfoUpdatedDomainEvent(
            tokenAddress,
            "OLD",
            "NEW",
            "Old Name",
            "New Name");

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(
            c => c.RemoveByPrefixAsync($"liquidity:token:{tokenAddress}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotifyClients()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var newSymbol = "WETH";
        var newName = "Wrapped Ether";
        var domainEvent = new TokenInfoUpdatedDomainEvent(
            tokenAddress,
            "OLD",
            newSymbol,
            "Old Name",
            newName);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            n => n.NotifyTokenUpdatedAsync(
                tokenAddress,
                newSymbol,
                newName,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheInvalidationFails_ShouldContinue()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var domainEvent = new TokenInfoUpdatedDomainEvent(
            tokenAddress,
            "OLD",
            "NEW",
            "Old Name",
            "New Name");

        _cacheServiceMock
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        // Act - should not throw
        var exception = await Record.ExceptionAsync(() =>
            _handler.Handle(domainEvent, CancellationToken.None));

        // Assert
        exception.Should().BeNull();
        _notificationServiceMock.Verify(
            n => n.NotifyTokenUpdatedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotificationFails_ShouldNotThrow()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var domainEvent = new TokenInfoUpdatedDomainEvent(
            tokenAddress,
            "OLD",
            "NEW",
            "Old Name",
            "New Name");

        _notificationServiceMock
            .Setup(n => n.NotifyTokenUpdatedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Notification error"));

        // Act - should not throw
        var exception = await Record.ExceptionAsync(() =>
            _handler.Handle(domainEvent, CancellationToken.None));

        // Assert
        exception.Should().BeNull();
    }
}
