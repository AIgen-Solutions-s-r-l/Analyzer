using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Entities;

namespace AnalyzerCore.Application.Pools.Queries.GetPoolsByToken;

/// <summary>
/// Query to get all pools containing a specific token.
/// </summary>
public sealed record GetPoolsByTokenQuery : IQuery<IReadOnlyList<Pool>>
{
    public required string TokenAddress { get; init; }
    public required string ChainId { get; init; }
}
