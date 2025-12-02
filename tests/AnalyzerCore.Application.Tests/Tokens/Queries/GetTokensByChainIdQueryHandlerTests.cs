using AnalyzerCore.Application.Tokens.Queries.GetTokensByChainId;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.Tokens.Queries;

public class GetTokensByChainIdQueryHandlerTests
{
    private readonly Mock<ITokenRepository> _tokenRepositoryMock;
    private readonly GetTokensByChainIdQueryHandler _handler;

    public GetTokensByChainIdQueryHandlerTests()
    {
        _tokenRepositoryMock = new Mock<ITokenRepository>();
        _handler = new GetTokensByChainIdQueryHandler(_tokenRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingTokens_ShouldReturnTokens()
    {
        // Arrange
        var chainId = "1";
        var tokens = new List<Token>
        {
            Token.CreateLegacy("0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2", "WETH", "Wrapped Ether", 18, chainId),
            Token.CreateLegacy("0xdac17f958d2ee523a2206206994597c13d831ec7", "USDT", "Tether", 6, chainId),
            Token.CreateLegacy("0x6b175474e89094c44da98b954eedeac495271d0f", "DAI", "Dai Stablecoin", 18, chainId)
        };

        var query = new GetTokensByChainIdQuery(chainId);

        _tokenRepositoryMock
            .Setup(r => r.GetAllByChainIdAsync(chainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithNoTokens_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetTokensByChainIdQuery("1");

        _tokenRepositoryMock
            .Setup(r => r.GetAllByChainIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Token>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithInvalidChainId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetTokensByChainIdQuery("");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithDifferentChainId_ShouldReturnCorrectTokens()
    {
        // Arrange
        var chainId = "56"; // BSC
        var tokens = new List<Token>
        {
            Token.CreateLegacy("0xbb4cdb9cbd36b01bd1cbaebf2de08d9173bc095c", "WBNB", "Wrapped BNB", 18, chainId)
        };

        var query = new GetTokensByChainIdQuery(chainId);

        _tokenRepositoryMock
            .Setup(r => r.GetAllByChainIdAsync(chainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Symbol.Should().Be("WBNB");
    }
}
