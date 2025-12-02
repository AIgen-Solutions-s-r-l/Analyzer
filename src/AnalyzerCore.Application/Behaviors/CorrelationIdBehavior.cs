using AnalyzerCore.Application.Abstractions;
using MediatR;
using Serilog.Context;

namespace AnalyzerCore.Application.Behaviors;

/// <summary>
/// Pipeline behavior that ensures the correlation ID is included in the logging context
/// for all MediatR requests, enabling distributed tracing.
/// </summary>
public sealed class CorrelationIdBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public CorrelationIdBehavior(ICorrelationIdAccessor correlationIdAccessor)
    {
        _correlationIdAccessor = correlationIdAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Ensure correlation ID is in the log context for MediatR handlers
        using (LogContext.PushProperty("CorrelationId", _correlationIdAccessor.CorrelationId))
        {
            return await next();
        }
    }
}
