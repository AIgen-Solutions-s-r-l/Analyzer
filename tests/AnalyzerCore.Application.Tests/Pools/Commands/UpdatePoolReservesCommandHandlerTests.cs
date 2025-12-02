using AnalyzerCore.Application.Pools.Commands.UpdatePoolReserves;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.Pools.Commands;

public class UpdatePoolReservesCommandHandlerTests
{
    private readonly Mock<IPoolRepository> _poolRepositoryMock;
    private readonly UpdatePoolReservesCommandHandler _handler;

    public UpdatePoolReservesCommandHandlerTests()
    {
        _poolRepositoryMock = new Mock<IPoolRepository>();
        _handler = new UpdatePoolReservesCommandHandler(
            _poolRepositoryMock.Object,
            NullLogger<UpdatePoolReservesCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithExistingPool_ShouldUpdateReserves()
    {
        // Arrange
        var poolAddress = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852";
        var factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f";

        var token0 = Token.CreateLegacy("0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2", "WETH", "Wrapped Ether", 18, "1");
        var token1 = Token.CreateLegacy("0xdac17f958d2ee523a2206206994597c13d831ec7", "USDT", "Tether", 6, "1");
        var pool = Pool.Create(
            EthereumAddress.Create(poolAddress).Value,
            token0,
            token1,
            EthereumAddress.Create(factory).Value,
            PoolType.UniswapV2).Value;

        var command = new UpdatePoolReservesCommand
        {
            Address = poolAddress,
            Factory = factory,
            Reserve0 = 1000000m,
            Reserve1 = 2000000m
        };

        _poolRepositoryMock
            .Setup(r => r.GetByAddressAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pool);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pool.Reserve0.Should().Be(1000000m);
        pool.Reserve1.Should().Be(2000000m);
    }

    [Fact]
    public async Task Handle_WithNonExistingPool_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdatePoolReservesCommand
        {
            Address = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
            Factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f",
            Reserve0 = 1000000m,
            Reserve1 = 2000000m
        };

        _poolRepositoryMock
            .Setup(r => r.GetByAddressAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pool?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DomainErrors.Pool.NotFound(command.Address).Code);
    }

    [Fact]
    public async Task Handle_WithInvalidAddress_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdatePoolReservesCommand
        {
            Address = "invalid-address",
            Factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f",
            Reserve0 = 1000000m,
            Reserve1 = 2000000m
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidFactory_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdatePoolReservesCommand
        {
            Address = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
            Factory = "invalid-factory",
            Reserve0 = 1000000m,
            Reserve1 = 2000000m
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNegativeReserves_ShouldReturnFailure()
    {
        // Arrange
        var poolAddress = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852";
        var factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f";

        var token0 = Token.CreateLegacy("0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2", "WETH", "Wrapped Ether", 18, "1");
        var token1 = Token.CreateLegacy("0xdac17f958d2ee523a2206206994597c13d831ec7", "USDT", "Tether", 6, "1");
        var pool = Pool.Create(
            EthereumAddress.Create(poolAddress).Value,
            token0,
            token1,
            EthereumAddress.Create(factory).Value,
            PoolType.UniswapV2).Value;

        var command = new UpdatePoolReservesCommand
        {
            Address = poolAddress,
            Factory = factory,
            Reserve0 = -1000m,
            Reserve1 = 2000000m
        };

        _poolRepositoryMock
            .Setup(r => r.GetByAddressAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pool);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
