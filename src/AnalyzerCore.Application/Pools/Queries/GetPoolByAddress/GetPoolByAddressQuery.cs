using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Entities;

namespace AnalyzerCore.Application.Pools.Queries.GetPoolByAddress;

/// <summary>
/// Query to get a pool by its address and factory.
/// </summary>
public sealed record GetPoolByAddressQuery : IQuery<Pool>
{
    public required string Address { get; init; }
    public required string Factory { get; init; }
}
