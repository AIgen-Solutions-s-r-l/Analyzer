using System.Diagnostics;
using AnalyzerCore.Domain.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Application.Behaviors;

/// <summary>
/// Pipeline behavior that logs request execution details.
/// Logs start, completion, duration, and errors.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation(
            "Processing request {RequestName}",
            requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await next();
            stopwatch.Stop();

            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Request {RequestName} failed with error {ErrorCode}: {ErrorMessage}. Duration: {Duration}ms",
                    requestName,
                    result.Error.Code,
                    result.Error.Message,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Request {RequestName} completed successfully. Duration: {Duration}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Request {RequestName} threw an exception. Duration: {Duration}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
