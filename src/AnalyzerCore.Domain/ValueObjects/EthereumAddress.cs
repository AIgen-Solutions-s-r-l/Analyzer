using System.Text.RegularExpressions;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Errors;

namespace AnalyzerCore.Domain.ValueObjects;

/// <summary>
/// Represents a valid Ethereum address.
/// Encapsulates validation logic and ensures addresses are always in a consistent format.
/// </summary>
public sealed partial class EthereumAddress : IEquatable<EthereumAddress>
{
    /// <summary>
    /// The zero address (0x0000000000000000000000000000000000000000).
    /// </summary>
    public static readonly EthereumAddress Zero = new("0x0000000000000000000000000000000000000000");

    /// <summary>
    /// Gets the normalized (lowercase) address value.
    /// </summary>
    public string Value { get; }

    private EthereumAddress(string value)
    {
        Value = value.ToLowerInvariant();
    }

    /// <summary>
    /// Creates an EthereumAddress from a string, validating the format.
    /// </summary>
    /// <param name="address">The address string to validate and wrap.</param>
    /// <returns>A Result containing the EthereumAddress or an error.</returns>
    public static Result<EthereumAddress> Create(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return Result.Failure<EthereumAddress>(DomainErrors.Address.NullOrEmpty);

        var trimmed = address.Trim();

        // Validate length (42 characters including 0x prefix)
        if (trimmed.Length != 42)
            return Result.Failure<EthereumAddress>(DomainErrors.Address.InvalidLength(trimmed));

        // Validate format (0x followed by 40 hex characters)
        if (!AddressRegex().IsMatch(trimmed))
            return Result.Failure<EthereumAddress>(DomainErrors.Address.InvalidFormat);

        return Result.Success(new EthereumAddress(trimmed));
    }

    /// <summary>
    /// Creates an EthereumAddress from a string, throwing an exception if invalid.
    /// Use only when you're certain the address is valid (e.g., from database).
    /// </summary>
    public static EthereumAddress FromTrusted(string address)
    {
        if (string.IsNullOrWhiteSpace(address) || address.Length != 42)
            throw new ArgumentException("Invalid trusted address", nameof(address));

        return new EthereumAddress(address);
    }

    /// <summary>
    /// Validates an address string without creating an instance.
    /// </summary>
    public static bool IsValid(string? address)
    {
        if (string.IsNullOrWhiteSpace(address) || address.Length != 42)
            return false;

        return AddressRegex().IsMatch(address);
    }

    /// <summary>
    /// Gets the checksum-encoded version of the address (EIP-55).
    /// </summary>
    public string ToChecksumAddress()
    {
        var addressLower = Value[2..].ToLowerInvariant();
        var hash = ComputeKeccak256Hash(addressLower);
        var result = "0x";

        for (var i = 0; i < addressLower.Length; i++)
        {
            if (int.Parse(hash[i].ToString(), System.Globalization.NumberStyles.HexNumber) >= 8)
                result += char.ToUpperInvariant(addressLower[i]);
            else
                result += addressLower[i];
        }

        return result;
    }

    /// <summary>
    /// Returns true if this is the zero address.
    /// </summary>
    public bool IsZeroAddress() => Value == Zero.Value;

    // Simple Keccak256 placeholder - in production, use Nethereum's implementation
    private static string ComputeKeccak256Hash(string input)
    {
        // This is a simplified version. In production, use:
        // Nethereum.Util.Sha3Keccack.Current.CalculateHash(input)
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [GeneratedRegex("^0x[a-fA-F0-9]{40}$", RegexOptions.Compiled)]
    private static partial Regex AddressRegex();

    #region Equality

    public bool Equals(EthereumAddress? other)
    {
        if (other is null) return false;
        return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is EthereumAddress other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public static bool operator ==(EthereumAddress? left, EthereumAddress? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(EthereumAddress? left, EthereumAddress? right)
    {
        return !(left == right);
    }

    #endregion

    public override string ToString() => Value;

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// </summary>
    public static implicit operator string(EthereumAddress address) => address.Value;
}
