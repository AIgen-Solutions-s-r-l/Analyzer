using System.Net;
using System.Net.Http.Json;
using AnalyzerCore.Api.Contracts.Tokens;
using FluentAssertions;
using Xunit;

namespace AnalyzerCore.Api.Tests.Controllers;

public class TokensControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TokensControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateToken_WithValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateTokenRequest
        {
            Address = "0x6b175474e89094c44da98b954eedeac495271d0f",
            ChainId = "1",
            Symbol = "DAI",
            Name = "Dai Stablecoin",
            Decimals = 18,
            TotalSupply = 1000000000
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tokens", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        token.Should().NotBeNull();
        token!.Address.Should().Be(request.Address.ToLowerInvariant());
        token.Symbol.Should().Be("DAI");
    }

    [Fact]
    public async Task CreateToken_WithInvalidAddress_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateTokenRequest
        {
            Address = "invalid-address",
            ChainId = "1",
            Symbol = "TEST",
            Name = "Test Token",
            Decimals = 18,
            TotalSupply = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tokens", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateToken_WithDuplicateToken_ShouldReturnConflict()
    {
        // Arrange
        var request = new CreateTokenRequest
        {
            Address = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            ChainId = "1",
            Symbol = "WETH",
            Name = "Wrapped Ether",
            Decimals = 18,
            TotalSupply = 1000000
        };

        // Create first token
        await _client.PostAsJsonAsync("/api/tokens", request);

        // Act - try to create duplicate
        var response = await _client.PostAsJsonAsync("/api/tokens", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetTokenByAddress_WithExistingToken_ShouldReturnToken()
    {
        // Arrange
        var createRequest = new CreateTokenRequest
        {
            Address = "0xdac17f958d2ee523a2206206994597c13d831ec7",
            ChainId = "1",
            Symbol = "USDT",
            Name = "Tether",
            Decimals = 6,
            TotalSupply = 50000000000
        };

        await _client.PostAsJsonAsync("/api/tokens", createRequest);

        // Act
        var response = await _client.GetAsync(
            $"/api/tokens/{createRequest.Address}?chainId={createRequest.ChainId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        token.Should().NotBeNull();
        token!.Symbol.Should().Be("USDT");
    }

    [Fact]
    public async Task GetTokenByAddress_WithNonExistingToken_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingAddress = "0x0000000000000000000000000000000000000001";

        // Act
        var response = await _client.GetAsync($"/api/tokens/{nonExistingAddress}?chainId=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTokensByChainId_ShouldReturnTokensForChain()
    {
        // Arrange
        var chainId = "56"; // BSC to avoid conflicts with other tests

        var request1 = new CreateTokenRequest
        {
            Address = "0xbb4cdb9cbd36b01bd1cbaebf2de08d9173bc095c",
            ChainId = chainId,
            Symbol = "WBNB",
            Name = "Wrapped BNB",
            Decimals = 18,
            TotalSupply = 1000000
        };

        var request2 = new CreateTokenRequest
        {
            Address = "0xe9e7cea3dedca5984780bafc599bd69add087d56",
            ChainId = chainId,
            Symbol = "BUSD",
            Name = "Binance USD",
            Decimals = 18,
            TotalSupply = 1000000000
        };

        await _client.PostAsJsonAsync("/api/tokens", request1);
        await _client.PostAsJsonAsync("/api/tokens", request2);

        // Act
        var response = await _client.GetAsync($"/api/tokens/chain/{chainId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await response.Content.ReadFromJsonAsync<List<TokenResponse>>();
        tokens.Should().NotBeNull();
        tokens!.Count.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task CreateToken_ShouldReturnCorrelationIdHeader()
    {
        // Arrange
        var request = new CreateTokenRequest
        {
            Address = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48",
            ChainId = "1",
            Symbol = "USDC",
            Name = "USD Coin",
            Decimals = 6,
            TotalSupply = 50000000000
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tokens", request);

        // Assert
        response.Headers.Should().ContainKey("X-Correlation-ID");
    }
}
