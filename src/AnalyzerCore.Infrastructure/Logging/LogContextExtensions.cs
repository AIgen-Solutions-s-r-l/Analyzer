using Serilog.Context;

namespace AnalyzerCore.Infrastructure.Logging;

/// <summary>
/// Extension methods for adding structured log context.
/// </summary>
public static class LogContextExtensions
{
    /// <summary>
    /// Creates a log scope for blockchain operations.
    /// </summary>
    public static IDisposable BeginBlockchainScope(
        string? chainId = null,
        string? blockNumber = null,
        string? transactionHash = null)
    {
        var disposables = new List<IDisposable>();

        if (!string.IsNullOrEmpty(chainId))
            disposables.Add(LogContext.PushProperty("ChainId", chainId));

        if (!string.IsNullOrEmpty(blockNumber))
            disposables.Add(LogContext.PushProperty("BlockNumber", blockNumber));

        if (!string.IsNullOrEmpty(transactionHash))
            disposables.Add(LogContext.PushProperty("TransactionHash", transactionHash));

        return new CompositeDisposable(disposables);
    }

    /// <summary>
    /// Creates a log scope for pool operations.
    /// </summary>
    public static IDisposable BeginPoolScope(
        string? poolAddress = null,
        string? token0 = null,
        string? token1 = null,
        string? dex = null)
    {
        var disposables = new List<IDisposable>();

        if (!string.IsNullOrEmpty(poolAddress))
            disposables.Add(LogContext.PushProperty("PoolAddress", poolAddress));

        if (!string.IsNullOrEmpty(token0))
            disposables.Add(LogContext.PushProperty("Token0", token0));

        if (!string.IsNullOrEmpty(token1))
            disposables.Add(LogContext.PushProperty("Token1", token1));

        if (!string.IsNullOrEmpty(dex))
            disposables.Add(LogContext.PushProperty("Dex", dex));

        return new CompositeDisposable(disposables);
    }

    /// <summary>
    /// Creates a log scope for token operations.
    /// </summary>
    public static IDisposable BeginTokenScope(
        string? tokenAddress = null,
        string? symbol = null)
    {
        var disposables = new List<IDisposable>();

        if (!string.IsNullOrEmpty(tokenAddress))
            disposables.Add(LogContext.PushProperty("TokenAddress", tokenAddress));

        if (!string.IsNullOrEmpty(symbol))
            disposables.Add(LogContext.PushProperty("TokenSymbol", symbol));

        return new CompositeDisposable(disposables);
    }

    /// <summary>
    /// Creates a log scope for user operations.
    /// </summary>
    public static IDisposable BeginUserScope(
        string? userId = null,
        string? userName = null,
        string? role = null)
    {
        var disposables = new List<IDisposable>();

        if (!string.IsNullOrEmpty(userId))
            disposables.Add(LogContext.PushProperty("UserId", userId));

        if (!string.IsNullOrEmpty(userName))
            disposables.Add(LogContext.PushProperty("UserName", userName));

        if (!string.IsNullOrEmpty(role))
            disposables.Add(LogContext.PushProperty("UserRole", role));

        return new CompositeDisposable(disposables);
    }

    /// <summary>
    /// Creates a log scope with correlation ID.
    /// </summary>
    public static IDisposable BeginCorrelationScope(string correlationId)
    {
        return LogContext.PushProperty("CorrelationId", correlationId);
    }

    /// <summary>
    /// Creates a log scope for command/query operations.
    /// </summary>
    public static IDisposable BeginOperationScope(
        string operationName,
        string? operationType = null)
    {
        var disposables = new List<IDisposable>
        {
            LogContext.PushProperty("OperationName", operationName)
        };

        if (!string.IsNullOrEmpty(operationType))
            disposables.Add(LogContext.PushProperty("OperationType", operationType));

        return new CompositeDisposable(disposables);
    }

    /// <summary>
    /// Helper class to dispose multiple disposables.
    /// </summary>
    private sealed class CompositeDisposable : IDisposable
    {
        private readonly IReadOnlyList<IDisposable> _disposables;

        public CompositeDisposable(IReadOnlyList<IDisposable> disposables)
        {
            _disposables = disposables;
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
