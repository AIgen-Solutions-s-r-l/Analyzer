using AnalyzerCore.Application.Pools.Queries.GetPoolsByToken;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.Pools.Queries;

public class GetPoolsByTokenQueryHandlerTests
{
    private readonly Mock<IPoolRepository> _poolRepositoryMock;
    private readonly GetPoolsByTokenQueryHandler _handler;

    public GetPoolsByTokenQueryHandlerTests()
    {
        _poolRepositoryMock = new Mock<IPoolRepository>();
        _handler = new GetPoolsByTokenQueryHandler(_poolRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingPools_ShouldReturnPools()
    {
        // Arrange
        var tokenAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
        var chainId = "1";

        var token0 = Token.CreateLegacy(tokenAddress, "WETH", "Wrapped Ether", 18, chainId);
        var token1 = Token.CreateLegacy("0xdac17f958d2ee523a2206206994597c13d831ec7", "USDT", "Tether", 6, chainId);

        var pools = new List<Pool>
        {
            Pool.Create(
                EthereumAddress.Create("0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852").Value,
                token0,
                token1,
                EthereumAddress.Create("0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f").Value,
                PoolType.UniswapV2).Value
        };

        var query = new GetPoolsByTokenQuery(tokenAddress, chainId);

        _poolRepositoryMock
            .Setup(r => r.GetPoolsByTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pools);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithNoPools_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetPoolsByTokenQuery(
            "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            "1");

        _poolRepositoryMock
            .Setup(r => r.GetPoolsByTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Pool>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithInvalidTokenAddress_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetPoolsByTokenQuery("invalid-address", "1");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DomainErrors.EthereumAddress.Invalid.Code);
    }

    [Fact]
    public async Task Handle_WithInvalidChainId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetPoolsByTokenQuery("0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2", "");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
