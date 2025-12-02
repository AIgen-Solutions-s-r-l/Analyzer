using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;

namespace AnalyzerCore.Infrastructure.Resilience;

/// <summary>
/// Factory for creating Polly resilience policies.
/// </summary>
public interface IResiliencePolicyFactory
{
    /// <summary>
    /// Creates a retry policy with exponential backoff.
    /// </summary>
    AsyncRetryPolicy CreateRetryPolicy();

    /// <summary>
    /// Creates a circuit breaker policy.
    /// </summary>
    AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy();

    /// <summary>
    /// Creates a timeout policy.
    /// </summary>
    AsyncTimeoutPolicy CreateTimeoutPolicy();

    /// <summary>
    /// Creates a bulkhead isolation policy.
    /// </summary>
    AsyncBulkheadPolicy CreateBulkheadPolicy();

    /// <summary>
    /// Creates a combined policy wrap with all policies.
    /// </summary>
    AsyncPolicyWrap CreateCombinedPolicy();

    /// <summary>
    /// Creates a typed retry policy.
    /// </summary>
    AsyncRetryPolicy<T> CreateRetryPolicy<T>();

    /// <summary>
    /// Creates a typed policy wrap.
    /// </summary>
    AsyncPolicyWrap<T> CreateCombinedPolicy<T>();
}

/// <summary>
/// Implementation of resilience policy factory.
/// </summary>
public sealed class ResiliencePolicyFactory : IResiliencePolicyFactory
{
    private readonly ResilienceOptions _options;
    private readonly ILogger<ResiliencePolicyFactory> _logger;
    private readonly Random _random = new();

    public ResiliencePolicyFactory(
        IOptions<ResilienceOptions> options,
        ILogger<ResiliencePolicyFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public AsyncRetryPolicy CreateRetryPolicy()
    {
        var retryOptions = _options.Retry;

        return Policy
            .Handle<Exception>(ex => IsTransientException(ex))
            .WaitAndRetryAsync(
                retryOptions.MaxRetries,
                retryAttempt => CalculateDelay(retryAttempt),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Retry attempt {RetryCount}/{MaxRetries} after {DelayMs}ms due to: {Message}",
                        retryCount,
                        retryOptions.MaxRetries,
                        timeSpan.TotalMilliseconds,
                        exception.Message);
                });
    }

    public AsyncRetryPolicy<T> CreateRetryPolicy<T>()
    {
        var retryOptions = _options.Retry;

        return Policy<T>
            .Handle<Exception>(ex => IsTransientException(ex))
            .WaitAndRetryAsync(
                retryOptions.MaxRetries,
                retryAttempt => CalculateDelay(retryAttempt),
                onRetry: (outcome, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        outcome.Exception,
                        "Retry attempt {RetryCount}/{MaxRetries} after {DelayMs}ms due to: {Message}",
                        retryCount,
                        retryOptions.MaxRetries,
                        timeSpan.TotalMilliseconds,
                        outcome.Exception?.Message ?? "Unknown error");
                });
    }

    public AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy()
    {
        var cbOptions = _options.CircuitBreaker;

        return Policy
            .Handle<Exception>(ex => IsTransientException(ex))
            .AdvancedCircuitBreakerAsync(
                failureThreshold: cbOptions.FailureThreshold / 100.0,
                samplingDuration: TimeSpan.FromSeconds(cbOptions.SamplingDurationSeconds),
                minimumThroughput: cbOptions.MinimumThroughput,
                durationOfBreak: TimeSpan.FromSeconds(cbOptions.DurationOfBreakSeconds),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError(
                        exception,
                        "Circuit breaker opened for {DurationSeconds}s due to: {Message}",
                        duration.TotalSeconds,
                        exception.Message);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset - resuming normal operations");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker half-open - testing with limited traffic");
                });
    }

    public AsyncTimeoutPolicy CreateTimeoutPolicy()
    {
        return Policy.TimeoutAsync(
            TimeSpan.FromSeconds(_options.Timeout.TimeoutSeconds),
            TimeoutStrategy.Optimistic,
            onTimeoutAsync: (context, timeSpan, task) =>
            {
                _logger.LogWarning(
                    "Operation timed out after {TimeoutSeconds}s",
                    timeSpan.TotalSeconds);
                return Task.CompletedTask;
            });
    }

    public AsyncBulkheadPolicy CreateBulkheadPolicy()
    {
        var bulkheadOptions = _options.Bulkhead;

        return Policy.BulkheadAsync(
            bulkheadOptions.MaxConcurrency,
            bulkheadOptions.MaxQueuingActions,
            onBulkheadRejectedAsync: context =>
            {
                _logger.LogWarning(
                    "Bulkhead rejected request - max concurrency {MaxConcurrency} exceeded",
                    bulkheadOptions.MaxConcurrency);
                return Task.CompletedTask;
            });
    }

    public AsyncPolicyWrap CreateCombinedPolicy()
    {
        // Order matters! Outer policies wrap inner ones
        // Bulkhead -> Timeout -> Circuit Breaker -> Retry
        return Policy.WrapAsync(
            CreateBulkheadPolicy(),
            CreateTimeoutPolicy(),
            CreateCircuitBreakerPolicy(),
            CreateRetryPolicy());
    }

    public AsyncPolicyWrap<T> CreateCombinedPolicy<T>()
    {
        return Policy.WrapAsync<T>(
            Policy<T>.BulkheadAsync(
                _options.Bulkhead.MaxConcurrency,
                _options.Bulkhead.MaxQueuingActions),
            Policy.TimeoutAsync<T>(
                TimeSpan.FromSeconds(_options.Timeout.TimeoutSeconds),
                TimeoutStrategy.Optimistic),
            CreateRetryPolicy<T>());
    }

    private TimeSpan CalculateDelay(int retryAttempt)
    {
        var retryOptions = _options.Retry;

        double delayMs;
        if (retryOptions.UseExponentialBackoff)
        {
            // Exponential backoff: initialDelay * 2^(attempt-1)
            delayMs = retryOptions.InitialDelayMs * Math.Pow(2, retryAttempt - 1);
        }
        else
        {
            // Linear backoff
            delayMs = retryOptions.InitialDelayMs * retryAttempt;
        }

        // Cap at maximum delay
        delayMs = Math.Min(delayMs, retryOptions.MaxDelayMs);

        // Add jitter to prevent thundering herd
        if (retryOptions.JitterFactor > 0)
        {
            var jitter = delayMs * retryOptions.JitterFactor * (_random.NextDouble() * 2 - 1);
            delayMs += jitter;
        }

        return TimeSpan.FromMilliseconds(Math.Max(delayMs, 100));
    }

    private static bool IsTransientException(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => true,
            TimeoutException => true,
            TaskCanceledException => true,
            OperationCanceledException => false, // Don't retry on cancellation
            InvalidOperationException ioe when ioe.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) => true,
            _ when ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) => true,
            _ when ex.Message.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase) => true,
            _ when ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }
}
