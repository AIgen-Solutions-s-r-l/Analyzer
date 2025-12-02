using AnalyzerCore.Application.EventHandlers;
using AnalyzerCore.Domain.Events;
using AnalyzerCore.Infrastructure.RealTime;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.EventHandlers;

public class PriceEventHandlerTests
{
    private readonly Mock<IRealtimeNotificationService> _notificationServiceMock;

    public PriceEventHandlerTests()
    {
        _notificationServiceMock = new Mock<IRealtimeNotificationService>();
    }

    #region PriceUpdatedDomainEventHandler Tests

    [Fact]
    public async Task PriceUpdatedHandler_ShouldBroadcastPriceUpdate()
    {
        // Arrange
        var handler = new PriceUpdatedDomainEventHandler(
            NullLogger<PriceUpdatedDomainEventHandler>.Instance,
            _notificationServiceMock.Object);

        var @event = new PriceUpdatedEvent
        {
            TokenAddress = "0x1234567890123456789012345678901234567890",
            TokenSymbol = "TEST",
            QuoteTokenSymbol = "ETH",
            OldPrice = 100m,
            NewPrice = 105m,
            PriceChangePercent = 5m,
            PriceUsd = 210m,
            PoolAddress = "0xpool",
            OccurredAt = DateTime.UtcNow
        };

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            x => x.BroadcastPriceUpdateAsync(It.Is<PriceUpdateMessage>(m =>
                m.TokenAddress == @event.TokenAddress &&
                m.TokenSymbol == @event.TokenSymbol &&
                m.OldPrice == @event.OldPrice &&
                m.NewPrice == @event.NewPrice)),
            Times.Once);
    }

    #endregion

    #region SignificantPriceChangeDomainEventHandler Tests

    [Fact]
    public async Task SignificantPriceChangeHandler_ShouldBroadcastAlert()
    {
        // Arrange
        var handler = new SignificantPriceChangeDomainEventHandler(
            NullLogger<SignificantPriceChangeDomainEventHandler>.Instance,
            _notificationServiceMock.Object);

        var @event = new SignificantPriceChangeEvent
        {
            TokenAddress = "0x1234567890123456789012345678901234567890",
            TokenSymbol = "TEST",
            OldPrice = 100m,
            NewPrice = 90m,
            PriceChangePercent = -10m,
            TimePeriod = TimeSpan.FromMinutes(30),
            OccurredAt = DateTime.UtcNow
        };

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            x => x.BroadcastAlertAsync(It.Is<AlertMessage>(m =>
                m.Type == "price_alert" &&
                m.Severity == "critical")),
            Times.Once);
    }

    [Fact]
    public async Task SignificantPriceChangeHandler_WithModeratChange_ShouldHaveWarningSeverity()
    {
        // Arrange
        var handler = new SignificantPriceChangeDomainEventHandler(
            NullLogger<SignificantPriceChangeDomainEventHandler>.Instance,
            _notificationServiceMock.Object);

        var @event = new SignificantPriceChangeEvent
        {
            TokenAddress = "0x1234567890123456789012345678901234567890",
            TokenSymbol = "TEST",
            OldPrice = 100m,
            NewPrice = 95m,
            PriceChangePercent = -5m,
            TimePeriod = TimeSpan.FromMinutes(30),
            OccurredAt = DateTime.UtcNow
        };

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            x => x.BroadcastAlertAsync(It.Is<AlertMessage>(m =>
                m.Severity == "warning")),
            Times.Once);
    }

    #endregion
}
