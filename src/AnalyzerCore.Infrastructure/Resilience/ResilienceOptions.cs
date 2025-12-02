using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Infrastructure.Resilience;

/// <summary>
/// Configuration options for resilience policies.
/// </summary>
public sealed class ResilienceOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Resilience";

    /// <summary>
    /// Retry policy options.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Circuit breaker policy options.
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Timeout policy options.
    /// </summary>
    public TimeoutOptions Timeout { get; set; } = new();

    /// <summary>
    /// Bulkhead policy options.
    /// </summary>
    public BulkheadOptions Bulkhead { get; set; } = new();
}

/// <summary>
/// Retry policy configuration.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    [Range(1, 10)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay between retries in milliseconds.
    /// </summary>
    [Range(100, 60000)]
    public int InitialDelayMs { get; set; } = 500;

    /// <summary>
    /// Maximum delay between retries in milliseconds.
    /// </summary>
    [Range(1000, 300000)]
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>
    /// Whether to use exponential backoff.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Jitter factor (0-1) to add randomization to delays.
    /// </summary>
    [Range(0, 1)]
    public double JitterFactor { get; set; } = 0.2;
}

/// <summary>
/// Circuit breaker policy configuration.
/// </summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>
    /// Number of consecutive failures before opening the circuit.
    /// </summary>
    [Range(1, 100)]
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Sampling duration in seconds for counting failures.
    /// </summary>
    [Range(1, 300)]
    public int SamplingDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Minimum throughput before the circuit can trip.
    /// </summary>
    [Range(1, 100)]
    public int MinimumThroughput { get; set; } = 3;

    /// <summary>
    /// Duration the circuit stays open before testing (seconds).
    /// </summary>
    [Range(5, 300)]
    public int DurationOfBreakSeconds { get; set; } = 30;
}

/// <summary>
/// Timeout policy configuration.
/// </summary>
public sealed class TimeoutOptions
{
    /// <summary>
    /// Timeout for individual operations in seconds.
    /// </summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Overall timeout for the entire operation chain in seconds.
    /// </summary>
    [Range(5, 600)]
    public int OverallTimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// Bulkhead policy configuration.
/// </summary>
public sealed class BulkheadOptions
{
    /// <summary>
    /// Maximum number of concurrent executions.
    /// </summary>
    [Range(1, 1000)]
    public int MaxConcurrency { get; set; } = 10;

    /// <summary>
    /// Maximum number of requests waiting for a slot.
    /// </summary>
    [Range(0, 10000)]
    public int MaxQueuingActions { get; set; } = 100;
}
