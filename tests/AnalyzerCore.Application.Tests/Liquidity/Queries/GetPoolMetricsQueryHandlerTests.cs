using AnalyzerCore.Application.Liquidity.Queries.GetPoolMetrics;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.Liquidity.Queries;

public class GetPoolMetricsQueryHandlerTests
{
    private readonly Mock<ILiquidityAnalyticsService> _liquidityServiceMock;
    private readonly GetPoolMetricsQueryHandler _handler;

    public GetPoolMetricsQueryHandlerTests()
    {
        _liquidityServiceMock = new Mock<ILiquidityAnalyticsService>();
        _handler = new GetPoolMetricsQueryHandler(_liquidityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidPool_ShouldReturnMetrics()
    {
        // Arrange
        var poolAddress = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852";
        var expectedMetrics = LiquidityMetrics.Create(
            poolAddress,
            "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            "WETH",
            "0xdac17f958d2ee523a2206206994597c13d831ec7",
            "USDT",
            100m,
            185000m,
            185000m,
            185000m,
            50000m,
            0.3m);

        var query = new GetPoolMetricsQuery(poolAddress);

        _liquidityServiceMock
            .Setup(s => s.GetPoolMetricsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedMetrics));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PoolAddress.Should().Be(poolAddress);
        result.Value.TvlUsd.Should().Be(370000m);
    }

    [Fact]
    public async Task Handle_WithInvalidAddress_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetPoolMetricsQuery("invalid-address");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenPoolNotFound_ShouldReturnFailure()
    {
        // Arrange
        var poolAddress = "0x0000000000000000000000000000000000000000";
        var query = new GetPoolMetricsQuery(poolAddress);

        _liquidityServiceMock
            .Setup(s => s.GetPoolMetricsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LiquidityMetrics>(
                new Error("Liquidity.PoolNotFound", "Pool not found")));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Liquidity.PoolNotFound");
    }
}
