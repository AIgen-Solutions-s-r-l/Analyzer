using AnalyzerCore.Application.Pools.Queries.GetPoolByAddress;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.Pools.Queries;

public class GetPoolByAddressQueryHandlerTests
{
    private readonly Mock<IPoolRepository> _poolRepositoryMock;
    private readonly GetPoolByAddressQueryHandler _handler;

    public GetPoolByAddressQueryHandlerTests()
    {
        _poolRepositoryMock = new Mock<IPoolRepository>();
        _handler = new GetPoolByAddressQueryHandler(_poolRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingPool_ShouldReturnPool()
    {
        // Arrange
        var poolAddress = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852";
        var factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f";

        var token0 = Token.CreateLegacy("0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2", "WETH", "Wrapped Ether", 18, "1");
        var token1 = Token.CreateLegacy("0xdac17f958d2ee523a2206206994597c13d831ec7", "USDT", "Tether", 6, "1");
        var expectedPool = Pool.Create(
            EthereumAddress.Create(poolAddress).Value,
            token0,
            token1,
            EthereumAddress.Create(factory).Value,
            PoolType.UniswapV2).Value;

        var query = new GetPoolByAddressQuery(poolAddress, factory);

        _poolRepositoryMock
            .Setup(r => r.GetByAddressAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPool);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().Be(poolAddress.ToLowerInvariant());
    }

    [Fact]
    public async Task Handle_WithNonExistingPool_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetPoolByAddressQuery(
            "0x0000000000000000000000000000000000000000",
            "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f");

        _poolRepositoryMock
            .Setup(r => r.GetByAddressAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pool?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DomainErrors.Pool.NotFound(query.Address).Code);
    }

    [Fact]
    public async Task Handle_WithInvalidAddress_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetPoolByAddressQuery("invalid-address", "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DomainErrors.EthereumAddress.Invalid.Code);
    }

    [Fact]
    public async Task Handle_WithInvalidFactory_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetPoolByAddressQuery("0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852", "invalid-factory");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
