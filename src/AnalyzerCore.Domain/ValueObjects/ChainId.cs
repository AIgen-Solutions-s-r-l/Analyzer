using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Errors;

namespace AnalyzerCore.Domain.ValueObjects;

/// <summary>
/// Represents a valid blockchain chain ID.
/// </summary>
public sealed class ChainId : IEquatable<ChainId>
{
    // Well-known chain IDs
    public static readonly ChainId Ethereum = new("1", "Ethereum Mainnet");
    public static readonly ChainId Goerli = new("5", "Goerli Testnet");
    public static readonly ChainId Sepolia = new("11155111", "Sepolia Testnet");
    public static readonly ChainId Polygon = new("137", "Polygon Mainnet");
    public static readonly ChainId Arbitrum = new("42161", "Arbitrum One");
    public static readonly ChainId Optimism = new("10", "Optimism");
    public static readonly ChainId BinanceSmartChain = new("56", "BSC Mainnet");
    public static readonly ChainId Avalanche = new("43114", "Avalanche C-Chain");
    public static readonly ChainId Base = new("8453", "Base");

    private static readonly Dictionary<string, ChainId> WellKnownChains = new()
    {
        { "1", Ethereum },
        { "5", Goerli },
        { "11155111", Sepolia },
        { "137", Polygon },
        { "42161", Arbitrum },
        { "10", Optimism },
        { "56", BinanceSmartChain },
        { "43114", Avalanche },
        { "8453", Base }
    };

    /// <summary>
    /// Gets the chain ID value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the human-readable name of the chain (if known).
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets whether this is a well-known chain.
    /// </summary>
    public bool IsWellKnown => Name is not null;

    private ChainId(string value, string? name = null)
    {
        Value = value;
        Name = name;
    }

    /// <summary>
    /// Creates a ChainId from a string value.
    /// </summary>
    public static Result<ChainId> Create(string? chainId)
    {
        if (string.IsNullOrWhiteSpace(chainId))
            return Result.Failure<ChainId>(DomainErrors.ChainId.NullOrEmpty);

        var trimmed = chainId.Trim();

        // Validate that it's a positive number
        if (!long.TryParse(trimmed, out var numericValue) || numericValue <= 0)
            return Result.Failure<ChainId>(DomainErrors.ChainId.InvalidFormat);

        // Return well-known chain if available
        if (WellKnownChains.TryGetValue(trimmed, out var wellKnown))
            return Result.Success(wellKnown);

        // Return custom chain
        return Result.Success(new ChainId(trimmed));
    }

    /// <summary>
    /// Creates a ChainId from a trusted source (e.g., database).
    /// </summary>
    public static ChainId FromTrusted(string chainId)
    {
        if (string.IsNullOrWhiteSpace(chainId))
            throw new ArgumentException("Chain ID cannot be null or empty", nameof(chainId));

        if (WellKnownChains.TryGetValue(chainId, out var wellKnown))
            return wellKnown;

        return new ChainId(chainId);
    }

    /// <summary>
    /// Gets a well-known chain by ID, or null if not found.
    /// </summary>
    public static ChainId? GetWellKnown(string chainId) =>
        WellKnownChains.GetValueOrDefault(chainId);

    /// <summary>
    /// Returns true if this is a testnet chain.
    /// </summary>
    public bool IsTestnet => Value is "5" or "11155111";

    /// <summary>
    /// Returns true if this is a mainnet chain.
    /// </summary>
    public bool IsMainnet => !IsTestnet && IsWellKnown;

    #region Equality

    public bool Equals(ChainId? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ChainId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ChainId? left, ChainId? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ChainId? left, ChainId? right)
    {
        return !(left == right);
    }

    #endregion

    public override string ToString() => Name ?? Value;

    /// <summary>
    /// Implicit conversion to string.
    /// </summary>
    public static implicit operator string(ChainId chainId) => chainId.Value;
}
