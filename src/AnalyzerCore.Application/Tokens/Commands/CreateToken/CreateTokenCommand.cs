using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Entities;

namespace AnalyzerCore.Application.Tokens.Commands.CreateToken;

/// <summary>
/// Command to create a new token.
/// </summary>
public sealed record CreateTokenCommand : ICommand<Token>
{
    public required string Address { get; init; }
    public required string Symbol { get; init; }
    public required string Name { get; init; }
    public int Decimals { get; init; }
    public decimal TotalSupply { get; init; }
    public required string ChainId { get; init; }
}
