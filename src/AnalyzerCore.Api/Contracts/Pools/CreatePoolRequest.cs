using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Api.Contracts.Pools;

/// <summary>
/// Request to create a new liquidity pool.
/// </summary>
public sealed record CreatePoolRequest
{
    /// <summary>
    /// The Ethereum address of the pool contract.
    /// </summary>
    /// <example>0x0d4a11d5EEaaC28EC3F61d100daF4d40471f1852</example>
    [Required]
    [StringLength(42, MinimumLength = 42)]
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// The address of the first token in the pair.
    /// </summary>
    /// <example>0xC02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2</example>
    [Required]
    [StringLength(42, MinimumLength = 42)]
    public string Token0Address { get; init; } = string.Empty;

    /// <summary>
    /// The address of the second token in the pair.
    /// </summary>
    /// <example>0xdAC17F958D2ee523a2206206994597C13D831ec7</example>
    [Required]
    [StringLength(42, MinimumLength = 42)]
    public string Token1Address { get; init; } = string.Empty;

    /// <summary>
    /// The factory contract that created this pool.
    /// </summary>
    /// <example>0x5C69bEe701ef814a2B6a3EDD4B1652CB9cc5aA6f</example>
    [Required]
    [StringLength(42, MinimumLength = 42)]
    public string Factory { get; init; } = string.Empty;

    /// <summary>
    /// The chain ID where the pool exists.
    /// </summary>
    /// <example>1</example>
    [Required]
    public string ChainId { get; init; } = string.Empty;
}
