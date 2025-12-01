namespace AnalyzerCore.Api.Contracts.Tokens;

/// <summary>
/// Token response DTO.
/// </summary>
public sealed record TokenResponse
{
    /// <summary>
    /// The unique identifier of the token.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// The Ethereum address of the token contract.
    /// </summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// The chain ID where the token exists.
    /// </summary>
    public string ChainId { get; init; } = string.Empty;

    /// <summary>
    /// The token symbol.
    /// </summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// The token name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The token decimals.
    /// </summary>
    public int Decimals { get; init; }

    /// <summary>
    /// Whether this is a placeholder token (metadata not yet fetched).
    /// </summary>
    public bool IsPlaceholder { get; init; }

    /// <summary>
    /// When the token was first tracked.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}
