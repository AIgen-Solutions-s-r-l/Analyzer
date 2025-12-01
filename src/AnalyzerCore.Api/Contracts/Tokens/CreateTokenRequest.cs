using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Api.Contracts.Tokens;

/// <summary>
/// Request to create a new token.
/// </summary>
public sealed record CreateTokenRequest
{
    /// <summary>
    /// The Ethereum address of the token contract.
    /// </summary>
    /// <example>0x6B175474E89094C44Da98b954EesedCDeB3d416</example>
    [Required]
    [StringLength(42, MinimumLength = 42)]
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// The chain ID where the token exists.
    /// </summary>
    /// <example>1</example>
    [Required]
    public string ChainId { get; init; } = string.Empty;

    /// <summary>
    /// The token symbol (optional, will be fetched from blockchain if not provided).
    /// </summary>
    /// <example>DAI</example>
    [StringLength(20)]
    public string? Symbol { get; init; }

    /// <summary>
    /// The token name (optional, will be fetched from blockchain if not provided).
    /// </summary>
    /// <example>Dai Stablecoin</example>
    [StringLength(100)]
    public string? Name { get; init; }

    /// <summary>
    /// The token decimals (optional, will be fetched from blockchain if not provided).
    /// </summary>
    /// <example>18</example>
    [Range(0, 18)]
    public int? Decimals { get; init; }
}
