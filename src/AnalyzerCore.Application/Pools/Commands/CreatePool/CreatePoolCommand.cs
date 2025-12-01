using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Pools.Commands.CreatePool;

/// <summary>
/// Command to create a new liquidity pool.
/// </summary>
public sealed record CreatePoolCommand : ICommand<Pool>
{
    public required string Address { get; init; }
    public required string Token0Address { get; init; }
    public required string Token1Address { get; init; }
    public required string Factory { get; init; }
    public PoolType Type { get; init; } = PoolType.UniswapV2;
    public required string ChainId { get; init; }
}
