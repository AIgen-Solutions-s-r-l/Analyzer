using AnalyzerCore.Application.Liquidity.Queries.GetTokenLiquidity;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.Liquidity.Queries;

public class GetTokenLiquidityQueryHandlerTests
{
    private readonly Mock<ILiquidityAnalyticsService> _liquidityServiceMock;
    private readonly GetTokenLiquidityQueryHandler _handler;

    public GetTokenLiquidityQueryHandlerTests()
    {
        _liquidityServiceMock = new Mock<ILiquidityAnalyticsService>();
        _handler = new GetTokenLiquidityQueryHandler(_liquidityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldReturnLiquiditySummary()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var expectedSummary = new TokenLiquiditySummary
        {
            TokenAddress = tokenAddress,
            TokenSymbol = "WETH",
            TotalLiquidityUsd = 1000000m,
            PoolCount = 5,
            TopPools = new List<PoolLiquiditySummary>
            {
                new()
                {
                    PoolAddress = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
                    PairedTokenAddress = "0xdac17f958d2ee523a2206206994597c13d831ec7",
                    PairedTokenSymbol = "USDT",
                    LiquidityUsd = 500000m,
                    SharePercent = 50m
                }
            },
            AverageLiquidityPerPool = 200000m,
            TotalVolume24hUsd = 50000m,
            Timestamp = DateTime.UtcNow
        };

        var query = new GetTokenLiquidityQuery(tokenAddress);

        _liquidityServiceMock
            .Setup(s => s.GetTokenLiquiditySummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedSummary));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TokenAddress.Should().Be(tokenAddress);
        result.Value.TotalLiquidityUsd.Should().Be(1000000m);
        result.Value.PoolCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithInvalidAddress_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetTokenLiquidityQuery("invalid-address");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithTokenWithNoLiquidity_ShouldReturnEmptySummary()
    {
        // Arrange
        var tokenAddress = "0x0000000000000000000000000000000000000001";
        var emptySummary = new TokenLiquiditySummary
        {
            TokenAddress = tokenAddress,
            TokenSymbol = "UNKNOWN",
            TotalLiquidityUsd = 0,
            PoolCount = 0,
            TopPools = Array.Empty<PoolLiquiditySummary>(),
            AverageLiquidityPerPool = 0,
            TotalVolume24hUsd = 0,
            Timestamp = DateTime.UtcNow
        };

        var query = new GetTokenLiquidityQuery(tokenAddress);

        _liquidityServiceMock
            .Setup(s => s.GetTokenLiquiditySummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(emptySummary));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalLiquidityUsd.Should().Be(0);
        result.Value.PoolCount.Should().Be(0);
    }
}
