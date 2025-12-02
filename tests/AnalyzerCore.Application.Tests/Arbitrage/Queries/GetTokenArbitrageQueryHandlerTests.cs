using AnalyzerCore.Application.Arbitrage.Queries.GetTokenArbitrage;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.Arbitrage.Queries;

public class GetTokenArbitrageQueryHandlerTests
{
    private readonly Mock<IArbitrageService> _arbitrageServiceMock;
    private readonly GetTokenArbitrageQueryHandler _handler;

    public GetTokenArbitrageQueryHandlerTests()
    {
        _arbitrageServiceMock = new Mock<IArbitrageService>();
        _handler = new GetTokenArbitrageQueryHandler(_arbitrageServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldReturnOpportunity()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var opportunity = ArbitrageOpportunity.Create(
            tokenAddress,
            "WETH",
            buyPrice: 1850m,
            sellPrice: 1860m,
            buyPool: "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
            sellPool: "0xb4e16d0168e52d35cacd2c6185b44281ec28c9dc",
            buyPoolLiquidity: 5000000m,
            sellPoolLiquidity: 4000000m,
            estimatedGasCostUsd: 5m);

        var query = new GetTokenArbitrageQuery(tokenAddress);

        _arbitrageServiceMock
            .Setup(s => s.FindArbitrageForTokenAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ArbitrageOpportunity?>(opportunity));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TokenAddress.Should().Be(tokenAddress);
        result.Value.IsProfitable.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNoOpportunity_ShouldReturnNull()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var query = new GetTokenArbitrageQuery(tokenAddress);

        _arbitrageServiceMock
            .Setup(s => s.FindArbitrageForTokenAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ArbitrageOpportunity?>(null));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithInvalidAddress_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetTokenArbitrageQuery("invalid-address");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldNormalizeAddress()
    {
        // Arrange
        var tokenAddress = "0xC02AAA39B223FE8D0A0E5C4F27EAD9083C756CC2"; // uppercase
        var query = new GetTokenArbitrageQuery(tokenAddress);

        _arbitrageServiceMock
            .Setup(s => s.FindArbitrageForTokenAsync(
                tokenAddress.ToLowerInvariant(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ArbitrageOpportunity?>(null));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _arbitrageServiceMock.Verify(
            s => s.FindArbitrageForTokenAsync(
                tokenAddress.ToLowerInvariant(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
