using System.Diagnostics;

namespace AnalyzerCore.Infrastructure.Telemetry;

/// <summary>
/// Centralized activity sources for distributed tracing.
/// </summary>
public static class ActivitySources
{
    /// <summary>
    /// Activity source for blockchain operations (RPC calls, block processing).
    /// </summary>
    public static readonly ActivitySource Blockchain = new("AnalyzerCore.Blockchain", "1.0.0");

    /// <summary>
    /// Activity source for domain operations (business logic).
    /// </summary>
    public static readonly ActivitySource Domain = new("AnalyzerCore.Domain", "1.0.0");

    /// <summary>
    /// Activity source for application layer operations (commands, queries).
    /// </summary>
    public static readonly ActivitySource Application = new("AnalyzerCore.Application", "1.0.0");

    /// <summary>
    /// Activity source for infrastructure operations (caching, messaging).
    /// </summary>
    public static readonly ActivitySource Infrastructure = new("AnalyzerCore.Infrastructure", "1.0.0");

    /// <summary>
    /// All activity source names for registration.
    /// </summary>
    public static readonly string[] AllSourceNames =
    {
        "AnalyzerCore.Blockchain",
        "AnalyzerCore.Domain",
        "AnalyzerCore.Application",
        "AnalyzerCore.Infrastructure"
    };
}

/// <summary>
/// Extension methods for Activity (spans) enrichment.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Sets blockchain-specific tags on an activity.
    /// </summary>
    public static Activity? SetBlockchainTags(
        this Activity? activity,
        string? chainId = null,
        string? blockNumber = null,
        string? transactionHash = null)
    {
        if (activity is null) return null;

        if (!string.IsNullOrEmpty(chainId))
            activity.SetTag("blockchain.chain_id", chainId);

        if (!string.IsNullOrEmpty(blockNumber))
            activity.SetTag("blockchain.block_number", blockNumber);

        if (!string.IsNullOrEmpty(transactionHash))
            activity.SetTag("blockchain.transaction_hash", transactionHash);

        return activity;
    }

    /// <summary>
    /// Sets pool-specific tags on an activity.
    /// </summary>
    public static Activity? SetPoolTags(
        this Activity? activity,
        string? poolAddress = null,
        string? dex = null,
        string? token0 = null,
        string? token1 = null)
    {
        if (activity is null) return null;

        if (!string.IsNullOrEmpty(poolAddress))
            activity.SetTag("pool.address", poolAddress);

        if (!string.IsNullOrEmpty(dex))
            activity.SetTag("pool.dex", dex);

        if (!string.IsNullOrEmpty(token0))
            activity.SetTag("pool.token0", token0);

        if (!string.IsNullOrEmpty(token1))
            activity.SetTag("pool.token1", token1);

        return activity;
    }

    /// <summary>
    /// Sets token-specific tags on an activity.
    /// </summary>
    public static Activity? SetTokenTags(
        this Activity? activity,
        string? tokenAddress = null,
        string? symbol = null,
        int? decimals = null)
    {
        if (activity is null) return null;

        if (!string.IsNullOrEmpty(tokenAddress))
            activity.SetTag("token.address", tokenAddress);

        if (!string.IsNullOrEmpty(symbol))
            activity.SetTag("token.symbol", symbol);

        if (decimals.HasValue)
            activity.SetTag("token.decimals", decimals.Value);

        return activity;
    }

    /// <summary>
    /// Sets RPC call tags on an activity.
    /// </summary>
    public static Activity? SetRpcTags(
        this Activity? activity,
        string? method = null,
        string? endpoint = null,
        int? retryCount = null)
    {
        if (activity is null) return null;

        if (!string.IsNullOrEmpty(method))
            activity.SetTag("rpc.method", method);

        if (!string.IsNullOrEmpty(endpoint))
            activity.SetTag("rpc.endpoint", endpoint);

        if (retryCount.HasValue)
            activity.SetTag("rpc.retry_count", retryCount.Value);

        return activity;
    }

    /// <summary>
    /// Records an exception on the activity.
    /// </summary>
    public static Activity? RecordException(this Activity? activity, Exception exception)
    {
        if (activity is null || exception is null) return null;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag("exception.type", exception.GetType().FullName);
        activity.SetTag("exception.message", exception.Message);
        activity.SetTag("exception.stacktrace", exception.StackTrace);

        return activity;
    }

    /// <summary>
    /// Marks the activity as successful.
    /// </summary>
    public static Activity? SetSuccess(this Activity? activity, string? description = null)
    {
        if (activity is null) return null;

        activity.SetStatus(ActivityStatusCode.Ok, description);
        return activity;
    }
}
