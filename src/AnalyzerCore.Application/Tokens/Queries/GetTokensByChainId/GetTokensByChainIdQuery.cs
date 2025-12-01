using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Entities;

namespace AnalyzerCore.Application.Tokens.Queries.GetTokensByChainId;

/// <summary>
/// Query to get all tokens on a specific chain.
/// </summary>
public sealed record GetTokensByChainIdQuery : IQuery<IReadOnlyList<Token>>
{
    public required string ChainId { get; init; }
}
