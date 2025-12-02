namespace AnalyzerCore.Infrastructure.RateLimiting;

/// <summary>
/// Rate limiter for blockchain RPC calls.
/// </summary>
public interface IRpcRateLimiter
{
    /// <summary>
    /// Acquires a permit to make an RPC call.
    /// Blocks until a permit is available or timeout.
    /// </summary>
    Task<bool> AcquireAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tries to acquire a permit immediately without waiting.
    /// </summary>
    bool TryAcquire();

    /// <summary>
    /// Gets the current number of available permits.
    /// </summary>
    int AvailablePermits { get; }

    /// <summary>
    /// Gets the current queue length (waiting requests).
    /// </summary>
    int QueueLength { get; }
}
