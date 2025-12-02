using AnalyzerCore.Application.Liquidity.Queries.GetTopPools;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.Liquidity.Queries;

public class GetTopPoolsQueryHandlerTests
{
    private readonly Mock<ILiquidityAnalyticsService> _liquidityServiceMock;
    private readonly GetTopPoolsQueryHandler _handler;

    public GetTopPoolsQueryHandlerTests()
    {
        _liquidityServiceMock = new Mock<ILiquidityAnalyticsService>();
        _handler = new GetTopPoolsQueryHandler(_liquidityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnTopPoolsByTvl()
    {
        // Arrange
        var pools = new List<LiquidityMetrics>
        {
            CreatePoolMetrics("0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852", "WETH", "USDT", 10000000m),
            CreatePoolMetrics("0xb4e16d0168e52d35cacd2c6185b44281ec28c9dc", "WETH", "USDC", 8000000m),
            CreatePoolMetrics("0xa478c2975ab1ea89e8196811f51a7b7ade33eb11", "WETH", "DAI", 5000000m)
        };

        var query = new GetTopPoolsQuery(Limit: 10);

        _liquidityServiceMock
            .Setup(s => s.GetTopPoolsByTvlAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<LiquidityMetrics>>(pools));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.First().TvlUsd.Should().Be(10000000m);
    }

    [Fact]
    public async Task Handle_WithLimit_ShouldRespectLimit()
    {
        // Arrange
        var query = new GetTopPoolsQuery(Limit: 5);

        _liquidityServiceMock
            .Setup(s => s.GetTopPoolsByTvlAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<LiquidityMetrics>>(new List<LiquidityMetrics>()));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _liquidityServiceMock.Verify(
            s => s.GetTopPoolsByTvlAsync(5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoPools_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetTopPoolsQuery(Limit: 10);

        _liquidityServiceMock
            .Setup(s => s.GetTopPoolsByTvlAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<LiquidityMetrics>>(new List<LiquidityMetrics>()));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    private static LiquidityMetrics CreatePoolMetrics(
        string poolAddress,
        string token0Symbol,
        string token1Symbol,
        decimal tvl)
    {
        return LiquidityMetrics.Create(
            poolAddress,
            "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            token0Symbol,
            "0xdac17f958d2ee523a2206206994597c13d831ec7",
            token1Symbol,
            tvl / 2,
            tvl / 2,
            tvl / 2,
            tvl / 2,
            tvl * 0.1m,
            0.3m);
    }
}
