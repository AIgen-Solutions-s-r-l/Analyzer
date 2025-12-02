using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using AnalyzerCore.Infrastructure.BackgroundServices;
using AnalyzerCore.Infrastructure.Configuration;
using AnalyzerCore.Infrastructure.RealTime;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AnalyzerCore.Infrastructure.Tests.BackgroundServices;

public class ArbitrageScannerServiceTests
{
    private readonly Mock<IArbitrageService> _arbitrageServiceMock;
    private readonly Mock<IRealtimeNotificationService> _notificationServiceMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly ArbitrageScannerOptions _options;

    public ArbitrageScannerServiceTests()
    {
        _arbitrageServiceMock = new Mock<IArbitrageService>();
        _notificationServiceMock = new Mock<IRealtimeNotificationService>();
        _publisherMock = new Mock<IPublisher>();

        _options = new ArbitrageScannerOptions
        {
            Enabled = true,
            ScanIntervalMs = 100, // Short interval for tests
            MinProfitUsd = 10m,
            LargeOpportunityThresholdUsd = 100m,
            MinConfidenceScore = 60,
            ErrorRetryDelayMs = 100,
            EnableTriangularScan = false, // Disable for simpler tests
            MaxCachedOpportunities = 100
        };
    }

    private ArbitrageScannerService CreateService()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _arbitrageServiceMock.Object);
        services.AddScoped(_ => _notificationServiceMock.Object);
        services.AddScoped(_ => _publisherMock.Object);

        var serviceProvider = services.BuildServiceProvider();

        return new ArbitrageScannerService(
            serviceProvider,
            Options.Create(_options),
            NullLogger<ArbitrageScannerService>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldNotScan()
    {
        // Arrange
        _options.Enabled = false;
        var service = CreateService();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(cts.Token);

        // Assert
        _arbitrageServiceMock.Verify(
            x => x.ScanForOpportunitiesAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_ShouldCallArbitrageService()
    {
        // Arrange
        _arbitrageServiceMock
            .Setup(x => x.ScanForOpportunitiesAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ArbitrageOpportunity>>.Success(
                new List<ArbitrageOpportunity>()));

        var service = CreateService();
        var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(12000); // Wait for initial delay (10s) + scan interval
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert
        _arbitrageServiceMock.Verify(
            x => x.ScanForOpportunitiesAsync(_options.MinProfitUsd, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOpportunitiesFound_ShouldPublishEvents()
    {
        // Arrange
        var opportunity = new ArbitrageOpportunity
        {
            Id = Guid.NewGuid(),
            TokenAddress = "0x1234567890123456789012345678901234567890",
            TokenSymbol = "TEST",
            SpreadPercent = 5m,
            ExpectedProfitUsd = 150m,
            NetProfitUsd = 120m,
            ConfidenceScore = 80,
            Path = new List<ArbitrageLeg>
            {
                new() { PoolAddress = "0xpool1" },
                new() { PoolAddress = "0xpool2" }
            }
        };

        _arbitrageServiceMock
            .Setup(x => x.ScanForOpportunitiesAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ArbitrageOpportunity>>.Success(
                new List<ArbitrageOpportunity> { opportunity }));

        var service = CreateService();
        var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(12000); // Wait for initial delay + scan
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - should publish event
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLargeOpportunityFound_ShouldPublishAlert()
    {
        // Arrange
        var largeOpportunity = new ArbitrageOpportunity
        {
            Id = Guid.NewGuid(),
            TokenAddress = "0x1234567890123456789012345678901234567890",
            TokenSymbol = "TEST",
            SpreadPercent = 10m,
            ExpectedProfitUsd = 500m,
            NetProfitUsd = 200m, // Above LargeOpportunityThresholdUsd
            ConfidenceScore = 80, // Above MinConfidenceScore
            Path = new List<ArbitrageLeg>
            {
                new() { PoolAddress = "0xpool1" },
                new() { PoolAddress = "0xpool2" }
            }
        };

        _arbitrageServiceMock
            .Setup(x => x.ScanForOpportunitiesAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ArbitrageOpportunity>>.Success(
                new List<ArbitrageOpportunity> { largeOpportunity }));

        var service = CreateService();
        var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(12000);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - should publish both regular and large alert events
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2)); // At least 2 events per opportunity
    }

    [Fact]
    public async Task ExecuteAsync_WhenScanFails_ShouldContinueScanning()
    {
        // Arrange
        var callCount = 0;
        _arbitrageServiceMock
            .Setup(x => x.ScanForOpportunitiesAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return Result<IReadOnlyList<ArbitrageOpportunity>>.Failure(
                        Error.Failure("Scan.Failed", "Test failure"));
                }
                return Result<IReadOnlyList<ArbitrageOpportunity>>.Success(
                    new List<ArbitrageOpportunity>());
            });

        var service = CreateService();
        var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(12500); // Wait for initial delay + 2+ scans
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - should have tried at least twice
        callCount.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateOpportunity_ShouldNotPublishTwice()
    {
        // Arrange
        var opportunity = new ArbitrageOpportunity
        {
            Id = Guid.NewGuid(),
            TokenAddress = "0x1234567890123456789012345678901234567890",
            TokenSymbol = "TEST",
            SpreadPercent = 5m,
            ExpectedProfitUsd = 50m,
            NetProfitUsd = 40m, // Same profit bucket
            ConfidenceScore = 70,
            Path = new List<ArbitrageLeg>
            {
                new() { PoolAddress = "0xpool1" }
            }
        };

        _arbitrageServiceMock
            .Setup(x => x.ScanForOpportunitiesAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ArbitrageOpportunity>>.Success(
                new List<ArbitrageOpportunity> { opportunity }));

        var publishCount = 0;
        _publisherMock
            .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback(() => publishCount++)
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(12500); // Wait for 2+ scans
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - should only publish once for duplicate opportunity
        publishCount.Should().Be(1);
    }
}
