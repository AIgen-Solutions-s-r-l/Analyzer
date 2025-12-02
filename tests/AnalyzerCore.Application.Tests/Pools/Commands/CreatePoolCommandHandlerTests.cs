using AnalyzerCore.Application.Pools.Commands.CreatePool;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.Pools.Commands;

public class CreatePoolCommandHandlerTests
{
    private readonly Mock<IPoolRepository> _poolRepositoryMock;
    private readonly Mock<ITokenRepository> _tokenRepositoryMock;
    private readonly CreatePoolCommandHandler _handler;

    public CreatePoolCommandHandlerTests()
    {
        _poolRepositoryMock = new Mock<IPoolRepository>();
        _tokenRepositoryMock = new Mock<ITokenRepository>();
        _handler = new CreatePoolCommandHandler(
            _poolRepositoryMock.Object,
            _tokenRepositoryMock.Object,
            NullLogger<CreatePoolCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreatePool()
    {
        // Arrange
        var command = new CreatePoolCommand
        {
            Address = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
            Token0Address = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            Token1Address = "0xdac17f958d2ee523a2206206994597c13d831ec7",
            Factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f",
            ChainId = "1",
            Type = PoolType.UniswapV2
        };

        _poolRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _tokenRepositoryMock
            .Setup(r => r.GetByAddressAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Token?)null);

        _tokenRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Token>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _poolRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Pool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().Be(command.Address.ToLowerInvariant());
        result.Value.Factory.Should().Be(command.Factory.ToLowerInvariant());
        _poolRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Pool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingPool_ShouldReturnExistingPool()
    {
        // Arrange
        var command = new CreatePoolCommand
        {
            Address = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
            Token0Address = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            Token1Address = "0xdac17f958d2ee523a2206206994597c13d831ec7",
            Factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f",
            ChainId = "1",
            Type = PoolType.UniswapV2
        };

        var existingToken0 = Token.CreateLegacy(command.Token0Address, "WETH", "Wrapped Ether", 18, "1");
        var existingToken1 = Token.CreateLegacy(command.Token1Address, "USDT", "Tether", 6, "1");
        var existingPool = Pool.Create(
            EthereumAddress.Create(command.Address).Value,
            existingToken0,
            existingToken1,
            EthereumAddress.Create(command.Factory).Value,
            PoolType.UniswapV2).Value;

        _poolRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _poolRepositoryMock
            .Setup(r => r.GetByAddressAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPool);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _poolRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Pool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidAddress_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreatePoolCommand
        {
            Address = "invalid-address",
            Token0Address = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            Token1Address = "0xdac17f958d2ee523a2206206994597c13d831ec7",
            Factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f",
            ChainId = "1",
            Type = PoolType.UniswapV2
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DomainErrors.EthereumAddress.Invalid.Code);
    }

    [Fact]
    public async Task Handle_WithInvalidFactory_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreatePoolCommand
        {
            Address = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
            Token0Address = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            Token1Address = "0xdac17f958d2ee523a2206206994597c13d831ec7",
            Factory = "invalid-factory",
            ChainId = "1",
            Type = PoolType.UniswapV2
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithExistingTokens_ShouldReuseTokens()
    {
        // Arrange
        var command = new CreatePoolCommand
        {
            Address = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
            Token0Address = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            Token1Address = "0xdac17f958d2ee523a2206206994597c13d831ec7",
            Factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f",
            ChainId = "1",
            Type = PoolType.UniswapV2
        };

        var existingToken0 = Token.CreateLegacy(command.Token0Address, "WETH", "Wrapped Ether", 18, "1");
        var existingToken1 = Token.CreateLegacy(command.Token1Address, "USDT", "Tether", 6, "1");

        _poolRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _tokenRepositoryMock
            .Setup(r => r.GetByAddressAsync(command.Token0Address.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken0);

        _tokenRepositoryMock
            .Setup(r => r.GetByAddressAsync(command.Token1Address.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken1);

        _poolRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Pool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _tokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Token>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
