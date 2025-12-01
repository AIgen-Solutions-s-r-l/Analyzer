using AnalyzerCore.Application.EventHandlers;
using AnalyzerCore.Domain.Events;
using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AnalyzerCore.Application.Tests.EventHandlers;

public class DomainEventHandlerTests
{
    [Fact]
    public async Task TokenCreatedDomainEventHandler_Handle_ShouldCompleteWithoutError()
    {
        // Arrange
        var handler = new TokenCreatedDomainEventHandler(
            NullLogger<TokenCreatedDomainEventHandler>.Instance);

        var domainEvent = new TokenCreatedDomainEvent(
            TokenAddress: "0x6b175474e89094c44da98b954eedeac495271d0f",
            Symbol: "DAI",
            Name: "Dai Stablecoin",
            Decimals: 18,
            ChainId: "1");

        // Act
        var act = () => handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TokenInfoUpdatedDomainEventHandler_Handle_ShouldCompleteWithoutError()
    {
        // Arrange
        var handler = new TokenInfoUpdatedDomainEventHandler(
            NullLogger<TokenInfoUpdatedDomainEventHandler>.Instance);

        var domainEvent = new TokenInfoUpdatedDomainEvent(
            TokenAddress: "0x6b175474e89094c44da98b954eedeac495271d0f",
            OldSymbol: "UNKNOWN",
            NewSymbol: "DAI",
            OldName: "Unknown Token",
            NewName: "Dai Stablecoin");

        // Act
        var act = () => handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PoolCreatedDomainEventHandler_Handle_ShouldCompleteWithoutError()
    {
        // Arrange
        var handler = new PoolCreatedDomainEventHandler(
            NullLogger<PoolCreatedDomainEventHandler>.Instance);

        var domainEvent = new PoolCreatedDomainEvent(
            PoolAddress: "0x1234567890abcdef1234567890abcdef12345678",
            Token0Address: "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
            Token1Address: "0x6b175474e89094c44da98b954eedeac495271d0f",
            FactoryAddress: "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f",
            PoolType: PoolType.UniswapV2);

        // Act
        var act = () => handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PoolReservesUpdatedDomainEventHandler_Handle_ShouldCompleteWithoutError()
    {
        // Arrange
        var handler = new PoolReservesUpdatedDomainEventHandler(
            NullLogger<PoolReservesUpdatedDomainEventHandler>.Instance);

        var domainEvent = new PoolReservesUpdatedDomainEvent(
            PoolAddress: "0x1234567890abcdef1234567890abcdef12345678",
            PreviousReserve0: 1000000m,
            PreviousReserve1: 2000000m,
            NewReserve0: 1100000m,
            NewReserve1: 1900000m);

        // Act
        var act = () => handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
