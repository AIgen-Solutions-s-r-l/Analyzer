namespace AnalyzerCore.Application.Abstractions;

/// <summary>
/// Provides access to the current correlation ID for distributed tracing.
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// Gets the current correlation ID.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Sets the correlation ID for the current context.
    /// </summary>
    void SetCorrelationId(string correlationId);
}
