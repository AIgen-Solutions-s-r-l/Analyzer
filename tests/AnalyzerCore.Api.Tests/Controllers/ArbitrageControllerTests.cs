using AnalyzerCore.Api.Controllers;
using AnalyzerCore.Api.Contracts.Arbitrage;
using AnalyzerCore.Application.Arbitrage.Queries.GetTokenArbitrage;
using AnalyzerCore.Application.Arbitrage.Queries.ScanArbitrage;
using AnalyzerCore.Application.Common;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AnalyzerCore.Api.Tests.Controllers;

public class ArbitrageControllerTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly Mock<IArbitrageService> _arbitrageServiceMock;
    private readonly ArbitrageController _controller;

    public ArbitrageControllerTests()
    {
        _senderMock = new Mock<ISender>();
        _arbitrageServiceMock = new Mock<IArbitrageService>();
        _controller = new ArbitrageController(_senderMock.Object, _arbitrageServiceMock.Object);
    }

    [Fact]
    public async Task Scan_WhenOpportunitiesFound_ShouldReturnOk()
    {
        // Arrange
        var opportunities = new List<ArbitrageOpportunity>
        {
            CreateArbitrageOpportunity()
        };
        _senderMock
            .Setup(s => s.Send(It.IsAny<ScanArbitrageQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<ArbitrageOpportunity>>.Success(opportunities));

        // Act
        var result = await _controller.Scan();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<ArbitrageOpportunityResponse>>().Subject;
        response.Should().HaveCount(1);
    }

    [Fact]
    public async Task Scan_WithMinProfitFilter_ShouldUseFilter()
    {
        // Arrange
        _senderMock
            .Setup(s => s.Send(It.Is<ScanArbitrageQuery>(q => q.MinProfitUsd == 50m), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<ArbitrageOpportunity>>.Success(new List<ArbitrageOpportunity>()));

        // Act
        await _controller.Scan(minProfitUsd: 50m);

        // Assert
        _senderMock.Verify(s => s.Send(
            It.Is<ScanArbitrageQuery>(q => q.MinProfitUsd == 50m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByToken_WhenOpportunitiesFound_ShouldReturnOk()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var opportunities = new List<ArbitrageOpportunity>
        {
            CreateArbitrageOpportunity(tokenAddress)
        };
        _senderMock
            .Setup(s => s.Send(It.IsAny<GetTokenArbitrageQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<ArbitrageOpportunity>>.Success(opportunities));

        // Act
        var result = await _controller.GetByToken(tokenAddress);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<ArbitrageOpportunityResponse>>().Subject;
        response.First().TokenAddress.Should().Be(tokenAddress);
    }

    [Fact]
    public async Task GetByToken_WhenInvalidAddress_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidAddress = "invalid";
        _senderMock
            .Setup(s => s.Send(It.IsAny<GetTokenArbitrageQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<ArbitrageOpportunity>>.Failure(
                Error.Validation("Address.Invalid", "Invalid address format")));

        // Act
        var result = await _controller.GetByToken(invalidAddress);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetTriangular_WhenOpportunitiesFound_ShouldReturnOk()
    {
        // Arrange
        var baseToken = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var opportunities = new List<ArbitrageOpportunity>
        {
            CreateArbitrageOpportunity()
        };
        _arbitrageServiceMock
            .Setup(s => s.FindTriangularOpportunitiesAsync(baseToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<ArbitrageOpportunity>>.Success(opportunities));

        // Act
        var result = await _controller.GetTriangular(baseToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<ArbitrageOpportunityResponse>>().Subject;
        response.Should().HaveCount(1);
    }

    [Fact]
    public async Task CalculateOptimal_WhenPoolsValid_ShouldReturnOptimalAmount()
    {
        // Arrange
        var buyPool = "0x1234567890123456789012345678901234567890";
        var sellPool = "0x0987654321098765432109876543210987654321";
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var calculation = new OptimalArbitrageCalculation(5.25m, 0.125m);

        _arbitrageServiceMock
            .Setup(s => s.CalculateOptimalAmountAsync(buyPool, sellPool, tokenAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OptimalArbitrageCalculation>.Success(calculation));

        // Act
        var result = await _controller.CalculateOptimal(buyPool, sellPool, tokenAddress);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<OptimalArbitrageResponse>().Subject;
        response.OptimalInputAmount.Should().Be(5.25m);
        response.ExpectedProfit.Should().Be(0.125m);
    }

    [Fact]
    public async Task CalculateOptimal_WhenPoolNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var buyPool = "0x0000000000000000000000000000000000000000";
        var sellPool = "0x0000000000000000000000000000000000000001";
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";

        _arbitrageServiceMock
            .Setup(s => s.CalculateOptimalAmountAsync(buyPool, sellPool, tokenAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OptimalArbitrageCalculation>.Failure(
                Error.NotFound("Pool.NotFound", "Pool not found")));

        // Act
        var result = await _controller.CalculateOptimal(buyPool, sellPool, tokenAddress);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static ArbitrageOpportunity CreateArbitrageOpportunity(
        string tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2")
    {
        return new ArbitrageOpportunity(
            Guid.NewGuid(),
            tokenAddress,
            "WETH",
            new List<ArbitrageLeg>
            {
                new("0x1111", "Uniswap", tokenAddress, "0x2222", 1.0m, 1000000m),
                new("0x3333", "SushiSwap", "0x2222", tokenAddress, 1.005m, 500000m)
            },
            1845.50m,
            1852.30m,
            0.37m,
            68.00m,
            5.25m,
            12.50m,
            55.50m,
            1.05m,
            true,
            0.85m,
            DateTime.UtcNow);
    }
}
