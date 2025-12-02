using AnalyzerCore.Application.Abstractions;

namespace AnalyzerCore.Infrastructure.Correlation;

/// <summary>
/// AsyncLocal-based implementation of ICorrelationIdAccessor.
/// Provides correlation ID that flows across async calls within the same logical context.
/// </summary>
public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    public string CorrelationId => _correlationId.Value ?? GenerateCorrelationId();

    public void SetCorrelationId(string correlationId)
    {
        _correlationId.Value = correlationId;
    }

    private static string GenerateCorrelationId()
    {
        var correlationId = Guid.NewGuid().ToString("N");
        _correlationId.Value = correlationId;
        return correlationId;
    }
}
