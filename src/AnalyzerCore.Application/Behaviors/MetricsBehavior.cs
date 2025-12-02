using System.Diagnostics;
using AnalyzerCore.Application.Abstractions.Messaging;
using MediatR;

namespace AnalyzerCore.Application.Behaviors;

/// <summary>
/// Pipeline behavior that collects metrics for command and query processing.
/// Records execution count and duration using action delegates.
/// </summary>
public sealed class MetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly Action<string>? _recordCommand;
    private readonly Action<string>? _recordQuery;
    private readonly Action<double, string>? _recordCommandDuration;
    private readonly Action<double, string>? _recordQueryDuration;

    public MetricsBehavior(
        Action<string>? recordCommand = null,
        Action<string>? recordQuery = null,
        Action<double, string>? recordCommandDuration = null,
        Action<double, string>? recordQueryDuration = null)
    {
        _recordCommand = recordCommand;
        _recordQuery = recordQuery;
        _recordCommandDuration = recordCommandDuration;
        _recordQueryDuration = recordQueryDuration;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;
        var isCommand = typeof(TRequest).GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await next();
        }
        finally
        {
            stopwatch.Stop();
            var durationSeconds = stopwatch.Elapsed.TotalSeconds;

            if (isCommand)
            {
                _recordCommand?.Invoke(requestType);
                _recordCommandDuration?.Invoke(durationSeconds, requestType);
            }
            else
            {
                _recordQuery?.Invoke(requestType);
                _recordQueryDuration?.Invoke(durationSeconds, requestType);
            }
        }
    }
}
