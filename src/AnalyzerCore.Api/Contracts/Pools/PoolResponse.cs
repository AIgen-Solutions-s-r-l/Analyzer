namespace AnalyzerCore.Api.Contracts.Pools;

/// <summary>
/// Pool response DTO.
/// </summary>
public sealed record PoolResponse
{
    /// <summary>
    /// The unique identifier of the pool.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// The Ethereum address of the pool contract.
    /// </summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// The chain ID where the pool exists.
    /// </summary>
    public string ChainId { get; init; } = string.Empty;

    /// <summary>
    /// The factory contract that created this pool.
    /// </summary>
    public string Factory { get; init; } = string.Empty;

    /// <summary>
    /// The address of the first token.
    /// </summary>
    public string Token0Address { get; init; } = string.Empty;

    /// <summary>
    /// The address of the second token.
    /// </summary>
    public string Token1Address { get; init; } = string.Empty;

    /// <summary>
    /// The current reserve of token0.
    /// </summary>
    public decimal Reserve0 { get; init; }

    /// <summary>
    /// The current reserve of token1.
    /// </summary>
    public decimal Reserve1 { get; init; }

    /// <summary>
    /// The pool type.
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// When the pool was first tracked.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the pool reserves were last updated.
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}
