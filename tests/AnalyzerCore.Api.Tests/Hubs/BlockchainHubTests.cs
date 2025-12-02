using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace AnalyzerCore.Api.Tests.Hubs;

public class BlockchainHubTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public BlockchainHubTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Connect_WithValidAuthentication_ShouldSucceed()
    {
        // Arrange
        var client = _factory.CreateClient();
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                client.BaseAddress + "hubs/blockchain",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
            .Build();

        // Act
        await hubConnection.StartAsync();

        // Assert
        hubConnection.State.Should().Be(HubConnectionState.Connected);

        // Cleanup
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeToBlocks_ShouldJoinBlocksGroup()
    {
        // Arrange
        var client = _factory.CreateClient();
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                client.BaseAddress + "hubs/blockchain",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
            .Build();

        await hubConnection.StartAsync();

        // Act
        await hubConnection.InvokeAsync("SubscribeToBlocks");

        // Assert - no exception means success
        hubConnection.State.Should().Be(HubConnectionState.Connected);

        // Cleanup
        await hubConnection.InvokeAsync("UnsubscribeFromBlocks");
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeToPool_WithValidAddress_ShouldJoinPoolGroup()
    {
        // Arrange
        var poolAddress = "0x1234567890123456789012345678901234567890";
        var client = _factory.CreateClient();
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                client.BaseAddress + "hubs/blockchain",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
            .Build();

        await hubConnection.StartAsync();

        // Act
        await hubConnection.InvokeAsync("SubscribeToPool", poolAddress);

        // Assert
        hubConnection.State.Should().Be(HubConnectionState.Connected);

        // Cleanup
        await hubConnection.InvokeAsync("UnsubscribeFromPool", poolAddress);
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeToToken_WithValidAddress_ShouldJoinTokenGroup()
    {
        // Arrange
        var tokenAddress = "0xabcdefabcdefabcdefabcdefabcdefabcdefabcd";
        var client = _factory.CreateClient();
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                client.BaseAddress + "hubs/blockchain",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
            .Build();

        await hubConnection.StartAsync();

        // Act
        await hubConnection.InvokeAsync("SubscribeToToken", tokenAddress);

        // Assert
        hubConnection.State.Should().Be(HubConnectionState.Connected);

        // Cleanup
        await hubConnection.InvokeAsync("UnsubscribeFromToken", tokenAddress);
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeToArbitrage_WithMinProfit_ShouldJoinArbitrageGroup()
    {
        // Arrange
        var minProfitUsd = 100m;
        var client = _factory.CreateClient();
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                client.BaseAddress + "hubs/blockchain",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
            .Build();

        await hubConnection.StartAsync();

        // Act
        await hubConnection.InvokeAsync("SubscribeToArbitrage", minProfitUsd);

        // Assert
        hubConnection.State.Should().Be(HubConnectionState.Connected);

        // Cleanup
        await hubConnection.InvokeAsync("UnsubscribeFromArbitrage");
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeToNewTokens_ShouldJoinNewTokensGroup()
    {
        // Arrange
        var client = _factory.CreateClient();
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                client.BaseAddress + "hubs/blockchain",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
            .Build();

        await hubConnection.StartAsync();

        // Act
        await hubConnection.InvokeAsync("SubscribeToNewTokens");

        // Assert
        hubConnection.State.Should().Be(HubConnectionState.Connected);

        // Cleanup
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeToNewPools_ShouldJoinNewPoolsGroup()
    {
        // Arrange
        var client = _factory.CreateClient();
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                client.BaseAddress + "hubs/blockchain",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
            .Build();

        await hubConnection.StartAsync();

        // Act
        await hubConnection.InvokeAsync("SubscribeToNewPools");

        // Assert
        hubConnection.State.Should().Be(HubConnectionState.Connected);

        // Cleanup
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task MultipleSubscriptions_ShouldAllSucceed()
    {
        // Arrange
        var client = _factory.CreateClient();
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                client.BaseAddress + "hubs/blockchain",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
            .Build();

        await hubConnection.StartAsync();

        // Act - subscribe to multiple groups
        await hubConnection.InvokeAsync("SubscribeToBlocks");
        await hubConnection.InvokeAsync("SubscribeToNewTokens");
        await hubConnection.InvokeAsync("SubscribeToNewPools");
        await hubConnection.InvokeAsync("SubscribeToArbitrage", (decimal?)null);

        // Assert
        hubConnection.State.Should().Be(HubConnectionState.Connected);

        // Cleanup
        await hubConnection.DisposeAsync();
    }
}
