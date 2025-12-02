using AnalyzerCore.Application.Prices.Queries.GetTokenPrice;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.Prices.Queries;

public class GetTokenPriceQueryHandlerTests
{
    private readonly Mock<IPriceService> _priceServiceMock;
    private readonly GetTokenPriceQueryHandler _handler;

    public GetTokenPriceQueryHandlerTests()
    {
        _priceServiceMock = new Mock<IPriceService>();
        _handler = new GetTokenPriceQueryHandler(_priceServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidAddress_ShouldReturnPrice()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var quoteAddress = "0xdac17f958d2ee523a2206206994597c13d831ec7";
        var expectedPrice = TokenPrice.Create(
            tokenAddress,
            quoteAddress,
            "USDT",
            1850.50m,
            1850.50m,
            "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
            1000000m,
            DateTime.UtcNow);

        var query = new GetTokenPriceQuery(tokenAddress, quoteAddress);

        _priceServiceMock
            .Setup(s => s.GetTokenPriceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedPrice));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TokenAddress.Should().Be(tokenAddress);
        result.Value.Price.Should().Be(1850.50m);
    }

    [Fact]
    public async Task Handle_WithInvalidAddress_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetTokenPriceQuery("invalid-address", "USDT");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Address");
    }

    [Fact]
    public async Task Handle_WhenPriceServiceFails_ShouldReturnFailure()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var query = new GetTokenPriceQuery(tokenAddress, "USDT");

        _priceServiceMock
            .Setup(s => s.GetTokenPriceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<TokenPrice>(new Error("Price.NotFound", "Price not found")));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Price.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldNormalizeAddress()
    {
        // Arrange
        var tokenAddress = "0xC02AAA39B223FE8D0A0E5C4F27EAD9083C756CC2"; // uppercase
        var expectedPrice = TokenPrice.Create(
            tokenAddress.ToLowerInvariant(),
            "0xdac17f958d2ee523a2206206994597c13d831ec7",
            "USDT",
            1850.50m,
            1850.50m,
            "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
            1000000m,
            DateTime.UtcNow);

        var query = new GetTokenPriceQuery(tokenAddress, "USDT");

        _priceServiceMock
            .Setup(s => s.GetTokenPriceAsync(
                It.Is<string>(addr => addr == tokenAddress.ToLowerInvariant()),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedPrice));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _priceServiceMock.Verify(
            s => s.GetTokenPriceAsync(
                tokenAddress.ToLowerInvariant(),
                "USDT",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
