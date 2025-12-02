using AnalyzerCore.Api.Controllers;
using AnalyzerCore.Api.Contracts.Prices;
using AnalyzerCore.Application.Common;
using AnalyzerCore.Application.Prices.Queries.GetPriceHistory;
using AnalyzerCore.Application.Prices.Queries.GetTokenPrice;
using AnalyzerCore.Application.Prices.Queries.GetTwap;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AnalyzerCore.Api.Tests.Controllers;

public class PricesControllerTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly Mock<IPriceService> _priceServiceMock;
    private readonly PricesController _controller;

    public PricesControllerTests()
    {
        _senderMock = new Mock<ISender>();
        _priceServiceMock = new Mock<IPriceService>();
        _controller = new PricesController(_senderMock.Object, _priceServiceMock.Object);
    }

    [Fact]
    public async Task GetPrice_WhenTokenExists_ShouldReturnOk()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var tokenPrice = CreateTokenPrice(tokenAddress);
        _senderMock
            .Setup(s => s.Send(It.IsAny<GetTokenPriceQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenPrice>.Success(tokenPrice));

        // Act
        var result = await _controller.GetPrice(tokenAddress);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TokenPriceResponse>().Subject;
        response.TokenAddress.Should().Be(tokenAddress);
    }

    [Fact]
    public async Task GetPrice_WhenTokenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var tokenAddress = "0x0000000000000000000000000000000000000000";
        _senderMock
            .Setup(s => s.Send(It.IsAny<GetTokenPriceQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenPrice>.Failure(Error.NotFound("Token.NotFound", "Token not found")));

        // Act
        var result = await _controller.GetPrice(tokenAddress);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetUsdPrice_WhenTokenExists_ShouldReturnOk()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var tokenPrice = CreateTokenPrice(tokenAddress, priceUsd: 1850.50m);
        _priceServiceMock
            .Setup(s => s.GetTokenPriceUsdAsync(tokenAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenPrice>.Success(tokenPrice));

        // Act
        var result = await _controller.GetUsdPrice(tokenAddress);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TokenPriceResponse>().Subject;
        response.PriceUsd.Should().Be(1850.50m);
    }

    [Fact]
    public async Task GetTwap_WhenDataAvailable_ShouldReturnOk()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var twapResult = new TwapResult(
            tokenAddress,
            "ETH",
            1848.25m,
            1850.50m,
            0.12m,
            TimeSpan.FromMinutes(60),
            60,
            DateTime.UtcNow);

        _senderMock
            .Setup(s => s.Send(It.IsAny<GetTwapQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TwapResult>.Success(twapResult));

        // Act
        var result = await _controller.GetTwap(tokenAddress, "ETH", 60);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TwapResponse>().Subject;
        response.TwapPrice.Should().Be(1848.25m);
        response.PeriodMinutes.Should().Be(60);
    }

    [Fact]
    public async Task GetPriceHistory_WhenDataAvailable_ShouldReturnOk()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var prices = new List<TokenPrice>
        {
            CreateTokenPrice(tokenAddress),
            CreateTokenPrice(tokenAddress)
        };

        _senderMock
            .Setup(s => s.Send(It.IsAny<GetPriceHistoryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<TokenPrice>>.Success(prices));

        // Act
        var result = await _controller.GetPriceHistory(tokenAddress);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<TokenPriceResponse>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPriceHistory_WithLimitParameter_ShouldRespectLimit()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        _senderMock
            .Setup(s => s.Send(It.Is<GetPriceHistoryQuery>(q => q.Limit == 50), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<TokenPrice>>.Success(new List<TokenPrice>()));

        // Act
        await _controller.GetPriceHistory(tokenAddress, limit: 50);

        // Assert
        _senderMock.Verify(s => s.Send(
            It.Is<GetPriceHistoryQuery>(q => q.Limit == 50),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetSupportedQuoteCurrencies_ShouldReturnCurrencies()
    {
        // Arrange
        var currencies = new[] { "ETH", "USDC", "USDT", "DAI", "USD" };
        _priceServiceMock
            .Setup(s => s.GetSupportedQuoteCurrencies())
            .Returns(currencies);

        // Act
        var result = _controller.GetSupportedQuoteCurrencies();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<string>>().Subject;
        response.Should().Contain("ETH");
        response.Should().Contain("USDC");
    }

    private static TokenPrice CreateTokenPrice(
        string tokenAddress,
        decimal price = 1.5m,
        decimal priceUsd = 1850.50m)
    {
        return new TokenPrice(
            tokenAddress,
            "0xdac17f958d2ee523a2206206994597c13d831ec7",
            "USDT",
            price,
            priceUsd,
            "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
            5000000m,
            DateTime.UtcNow);
    }
}
