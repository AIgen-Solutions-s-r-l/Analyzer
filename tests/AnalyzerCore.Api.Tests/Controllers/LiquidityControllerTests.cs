using AnalyzerCore.Api.Controllers;
using AnalyzerCore.Api.Contracts.Liquidity;
using AnalyzerCore.Application.Common;
using AnalyzerCore.Application.Liquidity.Queries.GetPoolMetrics;
using AnalyzerCore.Application.Liquidity.Queries.GetTokenLiquidity;
using AnalyzerCore.Application.Liquidity.Queries.GetTopPools;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AnalyzerCore.Api.Tests.Controllers;

public class LiquidityControllerTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly Mock<ILiquidityAnalyticsService> _liquidityServiceMock;
    private readonly LiquidityController _controller;

    public LiquidityControllerTests()
    {
        _senderMock = new Mock<ISender>();
        _liquidityServiceMock = new Mock<ILiquidityAnalyticsService>();
        _controller = new LiquidityController(_senderMock.Object, _liquidityServiceMock.Object);
    }

    [Fact]
    public async Task GetPoolMetrics_WhenPoolExists_ShouldReturnOk()
    {
        // Arrange
        var poolAddress = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852";
        var metrics = CreateLiquidityMetrics(poolAddress);
        _senderMock
            .Setup(s => s.Send(It.IsAny<GetPoolMetricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LiquidityMetrics>.Success(metrics));

        // Act
        var result = await _controller.GetPoolMetrics(poolAddress);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LiquidityMetricsResponse>().Subject;
        response.PoolAddress.Should().Be(poolAddress);
        response.TvlUsd.Should().Be(125000000m);
    }

    [Fact]
    public async Task GetPoolMetrics_WhenPoolNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var poolAddress = "0x0000000000000000000000000000000000000000";
        _senderMock
            .Setup(s => s.Send(It.IsAny<GetPoolMetricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LiquidityMetrics>.Failure(Error.NotFound("Pool.NotFound", "Pool not found")));

        // Act
        var result = await _controller.GetPoolMetrics(poolAddress);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTokenLiquidity_WhenTokenExists_ShouldReturnOk()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var summary = CreateTokenLiquiditySummary(tokenAddress);
        _senderMock
            .Setup(s => s.Send(It.IsAny<GetTokenLiquidityQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenLiquiditySummary>.Success(summary));

        // Act
        var result = await _controller.GetTokenLiquidity(tokenAddress);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TokenLiquiditySummaryResponse>().Subject;
        response.TokenAddress.Should().Be(tokenAddress);
        response.TotalLiquidityUsd.Should().Be(850000000m);
    }

    [Fact]
    public async Task GetTopPools_ShouldReturnOk()
    {
        // Arrange
        var pools = new List<LiquidityMetrics>
        {
            CreateLiquidityMetrics("0x1111111111111111111111111111111111111111"),
            CreateLiquidityMetrics("0x2222222222222222222222222222222222222222")
        };
        _senderMock
            .Setup(s => s.Send(It.IsAny<GetTopPoolsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<LiquidityMetrics>>.Success(pools));

        // Act
        var result = await _controller.GetTopPools();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LiquidityMetricsResponse>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTopPools_WithLimitParameter_ShouldRespectLimit()
    {
        // Arrange
        _senderMock
            .Setup(s => s.Send(It.Is<GetTopPoolsQuery>(q => q.Limit == 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<LiquidityMetrics>>.Success(new List<LiquidityMetrics>()));

        // Act
        await _controller.GetTopPools(limit: 5);

        // Assert
        _senderMock.Verify(s => s.Send(
            It.Is<GetTopPoolsQuery>(q => q.Limit == 5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalculateImpermanentLoss_WhenValid_ShouldReturnOk()
    {
        // Arrange
        var request = new ImpermanentLossRequest
        {
            PoolAddress = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
            EntryPriceRatio = 1850m,
            InitialInvestmentUsd = 10000m
        };
        var ilResult = new ImpermanentLossCalculation(
            request.PoolAddress,
            1850m,
            2200m,
            18.92m,
            0.83m,
            11892m,
            11793.27m,
            -98.73m,
            DateTime.UtcNow);

        _liquidityServiceMock
            .Setup(s => s.CalculateImpermanentLossAsync(
                request.PoolAddress, request.EntryPriceRatio, request.InitialInvestmentUsd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImpermanentLossCalculation>.Success(ilResult));

        // Act
        var result = await _controller.CalculateImpermanentLoss(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ImpermanentLossResponse>().Subject;
        response.ImpermanentLossPercent.Should().Be(0.83m);
    }

    [Fact]
    public async Task CalculateImpermanentLoss_WhenPoolNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var request = new ImpermanentLossRequest
        {
            PoolAddress = "0x0000000000000000000000000000000000000000",
            EntryPriceRatio = 1850m,
            InitialInvestmentUsd = 10000m
        };

        _liquidityServiceMock
            .Setup(s => s.CalculateImpermanentLossAsync(
                request.PoolAddress, request.EntryPriceRatio, request.InitialInvestmentUsd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImpermanentLossCalculation>.Failure(
                Error.NotFound("Pool.NotFound", "Pool not found")));

        // Act
        var result = await _controller.CalculateImpermanentLoss(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetLiquidityConcentration_WhenTokenExists_ShouldReturnOk()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var concentration = new LiquidityConcentration(
            tokenAddress,
            850000000m,
            0.147m,
            0.385m,
            0.512m,
            0.089m,
            "Low",
            245);

        _liquidityServiceMock
            .Setup(s => s.GetLiquidityConcentrationAsync(tokenAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LiquidityConcentration>.Success(concentration));

        // Act
        var result = await _controller.GetLiquidityConcentration(tokenAddress);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LiquidityConcentrationResponse>().Subject;
        response.HhiIndex.Should().Be(0.089m);
        response.ConcentrationLevel.Should().Be("Low");
    }

    private static LiquidityMetrics CreateLiquidityMetrics(string poolAddress)
    {
        return new LiquidityMetrics(
            poolAddress,
            "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            "WETH",
            "0xdac17f958d2ee523a2206206994597c13d831ec7",
            "USDT",
            125000000m,
            35000.50m,
            65000000m,
            62500000m,
            62500000m,
            45000000m,
            135000m,
            39.42m,
            0.92m,
            DateTime.UtcNow);
    }

    private static TokenLiquiditySummary CreateTokenLiquiditySummary(string tokenAddress)
    {
        return new TokenLiquiditySummary(
            tokenAddress,
            "WETH",
            850000000m,
            245,
            new List<PoolLiquiditySummary>
            {
                new("0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852", "0xdac17f958d2ee523a2206206994597c13d831ec7", "USDT", 125000000m, 14.71m)
            },
            3469387.76m,
            320000000m,
            DateTime.UtcNow);
    }
}
