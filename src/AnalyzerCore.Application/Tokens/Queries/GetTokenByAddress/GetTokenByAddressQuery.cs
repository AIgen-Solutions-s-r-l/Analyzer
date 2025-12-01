using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Entities;

namespace AnalyzerCore.Application.Tokens.Queries.GetTokenByAddress;

/// <summary>
/// Query to get a token by its address.
/// </summary>
public sealed record GetTokenByAddressQuery : IQuery<Token>
{
    public required string Address { get; init; }
    public required string ChainId { get; init; }
}
