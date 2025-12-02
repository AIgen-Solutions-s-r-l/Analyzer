using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Infrastructure.BackgroundServices;
using AnalyzerCore.Infrastructure.Blockchain;
using AnalyzerCore.Infrastructure.Configuration;
using AnalyzerCore.Infrastructure.RealTime;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AnalyzerCore.Infrastructure.Tests.BackgroundServices;

public class BlockchainMonitorServiceTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IBlockchainService> _blockchainServiceMock;
    private readonly Mock<IPoolRepository> _poolRepositoryMock;
    private readonly Mock<IRealtimeNotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<BlockchainMonitorService>> _loggerMock;

    public BlockchainMonitorServiceTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _blockchainServiceMock = new Mock<IBlockchainService>();
        _poolRepositoryMock = new Mock<IPoolRepository>();
        _notificationServiceMock = new Mock<IRealtimeNotificationService>();
        _loggerMock = new Mock<ILogger<BlockchainMonitorService>>();

        // Setup scope factory
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);

        // Setup service provider
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IBlockchainService)))
            .Returns(_blockchainServiceMock.Object);
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IPoolRepository)))
            .Returns(_poolRepositoryMock.Object);
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IRealtimeNotificationService)))
            .Returns(_notificationServiceMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var service = new BlockchainMonitorService(
            _scopeFactoryMock.Object,
            options,
            _loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateScope()
    {
        // Arrange
        var options = CreateOptions();
        var service = new BlockchainMonitorService(
            _scopeFactoryMock.Object,
            options,
            _loggerMock.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Setup blockchain service to return current block
        _blockchainServiceMock
            .Setup(b => b.GetCurrentBlockNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1000000L);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(50);
        await service.StopAsync(cts.Token);

        // Assert
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.AtLeastOnce);
    }

    private static IOptions<MonitoringOptions> CreateOptions()
    {
        return Options.Create(new MonitoringOptions
        {
            PollingInterval = 1000,
            BlocksToProcess = 5,
            BatchSize = 1,
            RetryDelay = 100,
            MaxRetries = 3
        });
    }
}
