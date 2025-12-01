using AnalyzerCore.Domain.Abstractions;

namespace AnalyzerCore.Domain.Errors;

/// <summary>
/// Domain-specific error definitions.
/// Organized by aggregate/entity for easy discovery and consistent error handling.
/// </summary>
public static class DomainErrors
{
    public static class Address
    {
        public static Error NullOrEmpty => new(
            "Address.NullOrEmpty",
            "The Ethereum address cannot be null or empty.");

        public static Error InvalidFormat => new(
            "Address.InvalidFormat",
            "The Ethereum address must be a valid 40-character hexadecimal string prefixed with '0x'.");

        public static Error InvalidChecksum => new(
            "Address.InvalidChecksum",
            "The Ethereum address has an invalid checksum.");

        public static Error InvalidLength(string address) => new(
            "Address.InvalidLength",
            $"The address '{address}' has an invalid length. Expected 42 characters (including '0x' prefix).");
    }

    public static class ChainId
    {
        public static Error NullOrEmpty => new(
            "ChainId.NullOrEmpty",
            "The chain ID cannot be null or empty.");

        public static Error InvalidFormat => new(
            "ChainId.InvalidFormat",
            "The chain ID must be a positive numeric value.");

        public static Error Unsupported(string chainId) => new(
            "ChainId.Unsupported",
            $"The chain ID '{chainId}' is not supported.");
    }

    public static class Token
    {
        public static Error NotFound(string address) => new(
            "Token.NotFound",
            $"The token with address '{address}' was not found.");

        public static Error AlreadyExists(string address) => new(
            "Token.AlreadyExists",
            $"A token with address '{address}' already exists.");

        public static Error InvalidDecimals => new(
            "Token.InvalidDecimals",
            "Token decimals must be between 0 and 18.");

        public static Error InvalidSymbol => new(
            "Token.InvalidSymbol",
            "Token symbol cannot be null or empty.");

        public static Error InvalidName => new(
            "Token.InvalidName",
            "Token name cannot be null or empty.");

        public static Error CreationFailed(string address, string reason) => new(
            "Token.CreationFailed",
            $"Failed to create token at '{address}': {reason}");
    }

    public static class Pool
    {
        public static Error NotFound(string address) => new(
            "Pool.NotFound",
            $"The pool with address '{address}' was not found.");

        public static Error AlreadyExists(string address, string factory) => new(
            "Pool.AlreadyExists",
            $"A pool with address '{address}' already exists for factory '{factory}'.");

        public static Error InvalidTokenPair => new(
            "Pool.InvalidTokenPair",
            "Token0 and Token1 addresses must be different.");

        public static Error InvalidFactory => new(
            "Pool.InvalidFactory",
            "The factory address cannot be null or empty.");

        public static Error InvalidReserves => new(
            "Pool.InvalidReserves",
            "Pool reserves cannot be negative.");

        public static Error TokenNotFound(string tokenAddress) => new(
            "Pool.TokenNotFound",
            $"Could not find or create token at address '{tokenAddress}'.");

        public static Error CreationFailed(string address, string reason) => new(
            "Pool.CreationFailed",
            $"Failed to create pool at '{address}': {reason}");
    }

    public static class Blockchain
    {
        public static Error ConnectionFailed(string rpcUrl) => new(
            "Blockchain.ConnectionFailed",
            $"Failed to connect to blockchain RPC at '{rpcUrl}'.");

        public static Error RpcError(string method, string message) => new(
            "Blockchain.RpcError",
            $"RPC call '{method}' failed: {message}");

        public static Error BlockNotFound(string blockNumber) => new(
            "Blockchain.BlockNotFound",
            $"Block '{blockNumber}' was not found.");

        public static Error TransactionNotFound(string txHash) => new(
            "Blockchain.TransactionNotFound",
            $"Transaction '{txHash}' was not found.");

        public static Error ContractCallFailed(string address, string method) => new(
            "Blockchain.ContractCallFailed",
            $"Contract call to '{method}' at '{address}' failed.");

        public static Error RateLimitExceeded => new(
            "Blockchain.RateLimitExceeded",
            "The RPC rate limit has been exceeded. Please try again later.");

        public static Error Timeout => new(
            "Blockchain.Timeout",
            "The blockchain operation timed out.");
    }
}
