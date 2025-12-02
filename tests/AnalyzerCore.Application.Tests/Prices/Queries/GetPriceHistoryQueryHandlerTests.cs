using AnalyzerCore.Application.Prices.Queries.GetPriceHistory;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.Prices.Queries;

public class GetPriceHistoryQueryHandlerTests
{
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepositoryMock;
    private readonly GetPriceHistoryQueryHandler _handler;

    public GetPriceHistoryQueryHandlerTests()
    {
        _priceHistoryRepositoryMock = new Mock<IPriceHistoryRepository>();
        _handler = new GetPriceHistoryQueryHandler(_priceHistoryRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidParameters_ShouldReturnHistory()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        var priceHistory = new List<PriceHistory>
        {
            CreatePriceHistory(tokenAddress, 1850m, DateTime.UtcNow.AddDays(-6)),
            CreatePriceHistory(tokenAddress, 1870m, DateTime.UtcNow.AddDays(-5)),
            CreatePriceHistory(tokenAddress, 1820m, DateTime.UtcNow.AddDays(-4)),
            CreatePriceHistory(tokenAddress, 1880m, DateTime.UtcNow.AddDays(-3)),
            CreatePriceHistory(tokenAddress, 1900m, DateTime.UtcNow.AddDays(-2))
        };

        var query = new GetPriceHistoryQuery(tokenAddress, "USDT", from, to, 100);

        _priceHistoryRepositoryMock
            .Setup(r => r.GetPriceHistoryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(priceHistory);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_WithInvalidAddress_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetPriceHistoryQuery(
            "invalid-address",
            "USDT",
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            100);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNoHistory_ShouldReturnEmptyList()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var query = new GetPriceHistoryQuery(
            tokenAddress,
            "USDT",
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            100);

        _priceHistoryRepositoryMock
            .Setup(r => r.GetPriceHistoryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceHistory>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldRespectLimit()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var query = new GetPriceHistoryQuery(
            tokenAddress,
            "USDT",
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            50);

        _priceHistoryRepositoryMock
            .Setup(r => r.GetPriceHistoryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                50,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceHistory>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _priceHistoryRepositoryMock.Verify(
            r => r.GetPriceHistoryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                50,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static PriceHistory CreatePriceHistory(string tokenAddress, decimal priceUsd, DateTime timestamp)
    {
        return PriceHistory.Create(
            tokenAddress,
            "0xdac17f958d2ee523a2206206994597c13d831ec7",
            "USDT",
            priceUsd,
            priceUsd,
            "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
            1000m,
            1000m * priceUsd,
            1000000m,
            timestamp);
    }
}
