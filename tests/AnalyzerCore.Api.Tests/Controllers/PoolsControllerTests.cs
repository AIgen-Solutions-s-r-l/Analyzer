using System.Net;
using System.Net.Http.Json;
using AnalyzerCore.Api.Contracts.Pools;
using AnalyzerCore.Api.Contracts.Tokens;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AnalyzerCore.Api.Tests.Controllers;

public class PoolsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PoolsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatePool_WithValidRequest_ShouldReturnCreated()
    {
        // Arrange - First create the tokens
        var token0Request = new CreateTokenRequest
        {
            Address = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            ChainId = "1",
            Symbol = "WETH",
            Name = "Wrapped Ether",
            Decimals = 18,
            TotalSupply = 1000000
        };

        var token1Request = new CreateTokenRequest
        {
            Address = "0x6b175474e89094c44da98b954eedeac495271d0f",
            ChainId = "1",
            Symbol = "DAI",
            Name = "Dai Stablecoin",
            Decimals = 18,
            TotalSupply = 1000000000
        };

        await _client.PostAsJsonAsync("/api/tokens", token0Request);
        await _client.PostAsJsonAsync("/api/tokens", token1Request);

        var poolRequest = new CreatePoolRequest
        {
            Address = "0xa478c2975ab1ea89e8196811f51a7b7ade33eb11",
            Token0Address = token0Request.Address,
            Token1Address = token1Request.Address,
            Factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f",
            ChainId = "1",
            Type = PoolType.UniswapV2
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pools", poolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var pool = await response.Content.ReadFromJsonAsync<PoolResponse>();
        pool.Should().NotBeNull();
        pool!.Address.Should().Be(poolRequest.Address.ToLowerInvariant());
    }

    [Fact]
    public async Task CreatePool_WithInvalidAddress_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreatePoolRequest
        {
            Address = "invalid-address",
            Token0Address = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            Token1Address = "0x6b175474e89094c44da98b954eedeac495271d0f",
            Factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f",
            ChainId = "1",
            Type = PoolType.UniswapV2
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pools", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPoolByAddress_WithExistingPool_ShouldReturnPool()
    {
        // Arrange - Create tokens first
        var token0Request = new CreateTokenRequest
        {
            Address = "0xdac17f958d2ee523a2206206994597c13d831ec7",
            ChainId = "1",
            Symbol = "USDT",
            Name = "Tether",
            Decimals = 6,
            TotalSupply = 50000000000
        };

        var token1Request = new CreateTokenRequest
        {
            Address = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48",
            ChainId = "1",
            Symbol = "USDC",
            Name = "USD Coin",
            Decimals = 6,
            TotalSupply = 50000000000
        };

        await _client.PostAsJsonAsync("/api/tokens", token0Request);
        await _client.PostAsJsonAsync("/api/tokens", token1Request);

        var poolRequest = new CreatePoolRequest
        {
            Address = "0x3041cbd36888becc7bbcbc0045e3b1f144466f5f",
            Token0Address = token0Request.Address,
            Token1Address = token1Request.Address,
            Factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f",
            ChainId = "1",
            Type = PoolType.UniswapV2
        };

        await _client.PostAsJsonAsync("/api/pools", poolRequest);

        // Act
        var response = await _client.GetAsync(
            $"/api/pools/{poolRequest.Address}?factory={poolRequest.Factory}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pool = await response.Content.ReadFromJsonAsync<PoolResponse>();
        pool.Should().NotBeNull();
        pool!.Address.Should().Be(poolRequest.Address.ToLowerInvariant());
    }

    [Fact]
    public async Task GetPoolByAddress_WithNonExistingPool_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingAddress = "0x0000000000000000000000000000000000000002";
        var factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f";

        // Act
        var response = await _client.GetAsync($"/api/pools/{nonExistingAddress}?factory={factory}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePoolReserves_WithExistingPool_ShouldReturnNoContent()
    {
        // Arrange - Create tokens and pool first
        var token0Request = new CreateTokenRequest
        {
            Address = "0x2260fac5e5542a773aa44fbcfedf7c193bc2c599",
            ChainId = "1",
            Symbol = "WBTC",
            Name = "Wrapped Bitcoin",
            Decimals = 8,
            TotalSupply = 21000000
        };

        var token1Request = new CreateTokenRequest
        {
            Address = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            ChainId = "1",
            Symbol = "WETH",
            Name = "Wrapped Ether",
            Decimals = 18,
            TotalSupply = 1000000
        };

        await _client.PostAsJsonAsync("/api/tokens", token0Request);
        await _client.PostAsJsonAsync("/api/tokens", token1Request);

        var poolRequest = new CreatePoolRequest
        {
            Address = "0xbb2b8038a1640196fbe3e38816f3e67cba72d940",
            Token0Address = token0Request.Address,
            Token1Address = token1Request.Address,
            Factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f",
            ChainId = "1",
            Type = PoolType.UniswapV2
        };

        await _client.PostAsJsonAsync("/api/pools", poolRequest);

        var updateRequest = new UpdatePoolReservesRequest
        {
            Reserve0 = 1000m,
            Reserve1 = 15000m
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/pools/{poolRequest.Address}/reserves?factory={poolRequest.Factory}",
            updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdatePoolReserves_WithNonExistingPool_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingAddress = "0x0000000000000000000000000000000000000003";
        var factory = "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f";

        var updateRequest = new UpdatePoolReservesRequest
        {
            Reserve0 = 1000m,
            Reserve1 = 15000m
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/pools/{nonExistingAddress}/reserves?factory={factory}",
            updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
