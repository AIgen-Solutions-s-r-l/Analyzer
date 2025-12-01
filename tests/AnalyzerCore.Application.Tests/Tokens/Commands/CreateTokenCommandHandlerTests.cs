using AnalyzerCore.Application.Tests.Common;
using AnalyzerCore.Application.Tokens.Commands.CreateToken;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AnalyzerCore.Application.Tests.Tokens.Commands;

public class CreateTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateToken()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var repository = new TokenRepository(context);
        var handler = new CreateTokenCommandHandler(repository, NullLogger<CreateTokenCommandHandler>.Instance);

        var command = new CreateTokenCommand
        {
            Address = "0x6B175474E89094C44Da98b954EedeAC495271d0F",
            ChainId = "1",
            Symbol = "DAI",
            Name = "Dai Stablecoin",
            Decimals = 18,
            TotalSupply = 1000000
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().Be(command.Address.ToLowerInvariant());
        result.Value.Symbol.Should().Be("DAI");
        result.Value.ChainId.Should().Be("1");
    }

    [Fact]
    public async Task Handle_WithDuplicateToken_ShouldReturnFailure()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var repository = new TokenRepository(context);
        var handler = new CreateTokenCommandHandler(repository, NullLogger<CreateTokenCommandHandler>.Instance);

        var command = new CreateTokenCommand
        {
            Address = "0x6B175474E89094C44Da98b954EedeAC495271d0F",
            ChainId = "1",
            Symbol = "DAI",
            Name = "Dai Stablecoin",
            Decimals = 18,
            TotalSupply = 1000000
        };

        // First creation should succeed
        await handler.Handle(command, CancellationToken.None);

        // Act - Second creation should fail
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DomainErrors.Token.AlreadyExists.Code);
    }

    [Fact]
    public async Task Handle_WithInvalidAddress_ShouldReturnFailure()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var repository = new TokenRepository(context);
        var handler = new CreateTokenCommandHandler(repository, NullLogger<CreateTokenCommandHandler>.Instance);

        var command = new CreateTokenCommand
        {
            Address = "invalid-address",
            ChainId = "1",
            Symbol = "TEST",
            Name = "Test Token",
            Decimals = 18,
            TotalSupply = 0
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
