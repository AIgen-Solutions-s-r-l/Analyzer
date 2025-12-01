using AnalyzerCore.Application.Tests.Common;
using AnalyzerCore.Application.Tokens.Queries.GetTokenByAddress;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Xunit;

namespace AnalyzerCore.Application.Tests.Tokens.Queries;

public class GetTokenByAddressQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingToken_ShouldReturnToken()
    {
        // Arrange
        var tokenAddress = "0x6b175474e89094c44da98b954eedeac495271d0f";
        var chainId = "1";

        await using var context = await TestDbContextFactory.CreateWithDataAsync(seedAction: async ctx =>
        {
            var token = Token.CreateLegacy(tokenAddress, "DAI", "Dai Stablecoin", 18, chainId);
            ctx.Tokens.Add(token);
        });

        var repository = new TokenRepository(context);
        var handler = new GetTokenByAddressQueryHandler(repository);

        var query = new GetTokenByAddressQuery(tokenAddress, chainId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().Be(tokenAddress);
        result.Value.Symbol.Should().Be("DAI");
    }

    [Fact]
    public async Task Handle_WithNonExistingToken_ShouldReturnFailure()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var repository = new TokenRepository(context);
        var handler = new GetTokenByAddressQueryHandler(repository);

        var query = new GetTokenByAddressQuery(
            "0x0000000000000000000000000000000000000000",
            "1");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DomainErrors.Token.NotFound.Code);
    }
}
