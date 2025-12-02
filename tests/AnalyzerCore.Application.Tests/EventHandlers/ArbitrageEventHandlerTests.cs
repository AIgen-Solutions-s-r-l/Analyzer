using AnalyzerCore.Application.EventHandlers;
using AnalyzerCore.Domain.Events;
using AnalyzerCore.Infrastructure.RealTime;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.EventHandlers;

public class ArbitrageEventHandlerTests
{
    private readonly Mock<IRealtimeNotificationService> _notificationServiceMock;

    public ArbitrageEventHandlerTests()
    {
        _notificationServiceMock = new Mock<IRealtimeNotificationService>();
    }

    #region ArbitrageOpportunityDetectedDomainEventHandler Tests

    [Fact]
    public async Task ArbitrageOpportunityDetectedHandler_ShouldBroadcastOpportunity()
    {
        // Arrange
        var handler = new ArbitrageOpportunityDetectedDomainEventHandler(
            NullLogger<ArbitrageOpportunityDetectedDomainEventHandler>.Instance,
            _notificationServiceMock.Object);

        var @event = new ArbitrageOpportunityDetectedEvent
        {
            OpportunityId = Guid.NewGuid(),
            TokenAddress = "0x1234567890123456789012345678901234567890",
            TokenSymbol = "TEST",
            SpreadPercent = 2.5m,
            ExpectedProfitUsd = 150m,
            NetProfitUsd = 100m,
            PathLength = 2,
            OccurredAt = DateTime.UtcNow
        };

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            x => x.BroadcastArbitrageOpportunityAsync(It.Is<ArbitrageOpportunityMessage>(m =>
                m.OpportunityId == @event.OpportunityId &&
                m.TokenSymbol == @event.TokenSymbol &&
                m.NetProfitUsd == @event.NetProfitUsd)),
            Times.Once);
    }

    #endregion

    #region LargeArbitrageAlertDomainEventHandler Tests

    [Fact]
    public async Task LargeArbitrageAlertHandler_ShouldBroadcastCriticalAlert()
    {
        // Arrange
        var handler = new LargeArbitrageAlertDomainEventHandler(
            NullLogger<LargeArbitrageAlertDomainEventHandler>.Instance,
            _notificationServiceMock.Object);

        var @event = new LargeArbitrageAlertEvent
        {
            OpportunityId = Guid.NewGuid(),
            TokenAddress = "0x1234567890123456789012345678901234567890",
            TokenSymbol = "TEST",
            NetProfitUsd = 5000m,
            SpreadPercent = 5m,
            ConfidenceScore = 85,
            OccurredAt = DateTime.UtcNow
        };

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            x => x.BroadcastAlertAsync(It.Is<AlertMessage>(m =>
                m.Type == "arbitrage_alert" &&
                m.Severity == "critical" &&
                m.Title.Contains("Large Arbitrage"))),
            Times.Once);
    }

    [Fact]
    public async Task LargeArbitrageAlertHandler_ShouldIncludeDataInAlert()
    {
        // Arrange
        var handler = new LargeArbitrageAlertDomainEventHandler(
            NullLogger<LargeArbitrageAlertDomainEventHandler>.Instance,
            _notificationServiceMock.Object);

        var opportunityId = Guid.NewGuid();
        var @event = new LargeArbitrageAlertEvent
        {
            OpportunityId = opportunityId,
            TokenAddress = "0x1234567890123456789012345678901234567890",
            TokenSymbol = "TEST",
            NetProfitUsd = 1000m,
            SpreadPercent = 3m,
            ConfidenceScore = 75,
            OccurredAt = DateTime.UtcNow
        };

        AlertMessage? capturedMessage = null;
        _notificationServiceMock
            .Setup(x => x.BroadcastAlertAsync(It.IsAny<AlertMessage>()))
            .Callback<AlertMessage>(m => capturedMessage = m)
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Data.Should().ContainKey("opportunityId");
        capturedMessage.Data.Should().ContainKey("netProfitUsd");
        capturedMessage.Data.Should().ContainKey("confidenceScore");
        capturedMessage.Data["opportunityId"].Should().Be(opportunityId);
        capturedMessage.Data["netProfitUsd"].Should().Be(1000m);
    }

    #endregion
}
