using AnalyzerCore.Application.Abstractions;
using Serilog.Context;

namespace AnalyzerCore.Api.Middleware;

/// <summary>
/// Middleware that extracts or generates a correlation ID for each request.
/// The correlation ID is added to the response headers and enriches all log messages.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string CorrelationIdLogPropertyName = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor correlationIdAccessor)
    {
        // Try to get correlation ID from request header, or generate a new one
        var correlationId = GetOrGenerateCorrelationId(context);

        // Set the correlation ID in the accessor (flows through async calls)
        correlationIdAccessor.SetCorrelationId(correlationId);

        // Add correlation ID to response header
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Enrich Serilog log context with correlation ID
        using (LogContext.PushProperty(CorrelationIdLogPropertyName, correlationId))
        {
            await _next(context);
        }
    }

    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId)
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}
