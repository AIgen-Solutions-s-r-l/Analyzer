namespace AnalyzerCore.Application.Abstractions.Caching;

/// <summary>
/// Centralized cache key management to avoid key collisions and ensure consistency.
/// </summary>
public static class CacheKeys
{
    private const string PoolPrefix = "pool";
    private const string TokenPrefix = "token";

    public static class Pools
    {
        public static string ByAddress(string address, string factory)
            => $"{PoolPrefix}:address:{address.ToLowerInvariant()}:factory:{factory}";

        public static string ByFactory(string factory)
            => $"{PoolPrefix}:factory:{factory}";

        public static string ByChainId(string chainId)
            => $"{PoolPrefix}:chain:{chainId}";

        public static string ByToken(string tokenAddress, string chainId)
            => $"{PoolPrefix}:token:{tokenAddress.ToLowerInvariant()}:chain:{chainId}";

        public static string Exists(string address, string factory)
            => $"{PoolPrefix}:exists:{address.ToLowerInvariant()}:factory:{factory}";

        public const string Prefix = PoolPrefix;
    }

    public static class Tokens
    {
        public static string ByAddress(string address, string chainId)
            => $"{TokenPrefix}:address:{address.ToLowerInvariant()}:chain:{chainId}";

        public static string ByChainId(string chainId)
            => $"{TokenPrefix}:chain:{chainId}";

        public static string Exists(string address, string chainId)
            => $"{TokenPrefix}:exists:{address.ToLowerInvariant()}:chain:{chainId}";

        public const string Prefix = TokenPrefix;
    }
}
