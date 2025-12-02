using AnalyzerCore.Application.Arbitrage.Queries.ScanArbitrage;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.Arbitrage.Queries;

public class ScanArbitrageQueryHandlerTests
{
    private readonly Mock<IArbitrageService> _arbitrageServiceMock;
    private readonly ScanArbitrageQueryHandler _handler;

    public ScanArbitrageQueryHandlerTests()
    {
        _arbitrageServiceMock = new Mock<IArbitrageService>();
        _handler = new ScanArbitrageQueryHandler(_arbitrageServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithOpportunities_ShouldReturnList()
    {
        // Arrange
        var opportunities = new List<ArbitrageOpportunity>
        {
            CreateOpportunity("0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2", "WETH", 100m, 5m),
            CreateOpportunity("0x514910771af9ca656af840dff83e8264ecf986ca", "LINK", 50m, 3m)
        };

        var query = new ScanArbitrageQuery(MinProfitUsd: 10m, Limit: 100);

        _arbitrageServiceMock
            .Setup(s => s.ScanArbitrageOpportunitiesAsync(
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<ArbitrageOpportunity>>(opportunities));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.First().TokenSymbol.Should().Be("WETH");
    }

    [Fact]
    public async Task Handle_WithNoOpportunities_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new ScanArbitrageQuery(MinProfitUsd: 1000m, Limit: 100);

        _arbitrageServiceMock
            .Setup(s => s.ScanArbitrageOpportunitiesAsync(
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<ArbitrageOpportunity>>(
                Array.Empty<ArbitrageOpportunity>()));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldRespectMinProfitFilter()
    {
        // Arrange
        var query = new ScanArbitrageQuery(MinProfitUsd: 50m, Limit: 100);

        _arbitrageServiceMock
            .Setup(s => s.ScanArbitrageOpportunitiesAsync(
                50m,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<ArbitrageOpportunity>>(
                new List<ArbitrageOpportunity>()));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _arbitrageServiceMock.Verify(
            s => s.ScanArbitrageOpportunitiesAsync(50m, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenServiceFails_ShouldReturnFailure()
    {
        // Arrange
        var query = new ScanArbitrageQuery(MinProfitUsd: 10m, Limit: 100);

        _arbitrageServiceMock
            .Setup(s => s.ScanArbitrageOpportunitiesAsync(
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<ArbitrageOpportunity>>(
                new Error("Arbitrage.ScanFailed", "Failed to scan arbitrage")));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Arbitrage.ScanFailed");
    }

    private static ArbitrageOpportunity CreateOpportunity(
        string tokenAddress,
        string tokenSymbol,
        decimal profitUsd,
        decimal spreadPercent)
    {
        return ArbitrageOpportunity.Create(
            tokenAddress,
            tokenSymbol,
            buyPrice: 100m,
            sellPrice: 100m * (1 + spreadPercent / 100),
            buyPool: "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
            sellPool: "0xb4e16d0168e52d35cacd2c6185b44281ec28c9dc",
            buyPoolLiquidity: 1000000m,
            sellPoolLiquidity: 1000000m,
            estimatedGasCostUsd: profitUsd * 0.1m);
    }
}
