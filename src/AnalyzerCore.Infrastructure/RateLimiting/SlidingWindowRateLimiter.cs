using System.Collections.Concurrent;
using AnalyzerCore.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalyzerCore.Infrastructure.RateLimiting;

/// <summary>
/// Sliding window rate limiter implementation for RPC calls.
/// Uses a combination of sliding window and semaphore for concurrent request limiting.
/// </summary>
public sealed class SlidingWindowRateLimiter : IRpcRateLimiter, IDisposable
{
    private readonly RateLimitOptions _options;
    private readonly ILogger<SlidingWindowRateLimiter> _logger;
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly ConcurrentQueue<DateTime> _requestTimestamps;
    private readonly SemaphoreSlim _queueSemaphore;
    private readonly object _lock = new();
    private int _queueLength;

    public SlidingWindowRateLimiter(
        IOptions<RateLimitOptions> options,
        ILogger<SlidingWindowRateLimiter> logger)
    {
        _options = options.Value;
        _logger = logger;
        _concurrencySemaphore = new SemaphoreSlim(_options.MaxConcurrentRequests, _options.MaxConcurrentRequests);
        _requestTimestamps = new ConcurrentQueue<DateTime>();
        _queueSemaphore = new SemaphoreSlim(_options.MaxQueueLength, _options.MaxQueueLength);
    }

    public int AvailablePermits => _concurrencySemaphore.CurrentCount;

    public int QueueLength => _queueLength;

    public async Task<bool> AcquireAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return true;
        }

        // Try to enter the queue
        if (_options.QueueExcessRequests)
        {
            if (!await _queueSemaphore.WaitAsync(0, cancellationToken))
            {
                _logger.LogWarning("Rate limiter queue is full. Rejecting request");
                return false;
            }

            Interlocked.Increment(ref _queueLength);
        }

        try
        {
            // Wait for concurrency slot
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.AcquireTimeoutMs);

            var acquired = await _concurrencySemaphore.WaitAsync(cts.Token);
            if (!acquired)
            {
                _logger.LogWarning("Failed to acquire rate limit permit within timeout");
                return false;
            }

            // Wait for sliding window slot
            await WaitForWindowSlotAsync(cts.Token);

            // Record the request
            RecordRequest();

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Rate limit acquire was cancelled or timed out");
            return false;
        }
        finally
        {
            if (_options.QueueExcessRequests)
            {
                Interlocked.Decrement(ref _queueLength);
                _queueSemaphore.Release();
            }
        }
    }

    public bool TryAcquire()
    {
        if (!_options.Enabled)
        {
            return true;
        }

        lock (_lock)
        {
            CleanupOldRequests();

            if (_requestTimestamps.Count >= _options.MaxRequestsPerWindow)
            {
                return false;
            }

            if (!_concurrencySemaphore.Wait(0))
            {
                return false;
            }

            RecordRequest();
            return true;
        }
    }

    public void Release()
    {
        _concurrencySemaphore.Release();
    }

    private async Task WaitForWindowSlotAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_lock)
            {
                CleanupOldRequests();

                if (_requestTimestamps.Count < _options.MaxRequestsPerWindow)
                {
                    return;
                }
            }

            // Wait a small amount before checking again
            await Task.Delay(50, cancellationToken);
        }
    }

    private void RecordRequest()
    {
        _requestTimestamps.Enqueue(DateTime.UtcNow);
    }

    private void CleanupOldRequests()
    {
        var windowStart = DateTime.UtcNow.AddSeconds(-_options.WindowSeconds);

        while (_requestTimestamps.TryPeek(out var timestamp) && timestamp < windowStart)
        {
            _requestTimestamps.TryDequeue(out _);
        }
    }

    public void Dispose()
    {
        _concurrencySemaphore.Dispose();
        _queueSemaphore.Dispose();
    }
}
