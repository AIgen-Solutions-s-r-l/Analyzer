using System.Diagnostics;
using AnalyzerCore.Domain.Abstractions;
using MediatR;

namespace AnalyzerCore.Application.Behaviors;

/// <summary>
/// Pipeline behavior that creates distributed tracing spans for each request.
/// Integrates with OpenTelemetry for distributed tracing.
/// </summary>
public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private static readonly ActivitySource ActivitySource = new("AnalyzerCore.Application", "1.0.0");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);
        var requestName = requestType.Name;

        // Determine if this is a command or query
        var isCommand = requestName.EndsWith("Command");
        var operationType = isCommand ? "command" : "query";

        using var activity = ActivitySource.StartActivity(
            $"{operationType}/{requestName}",
            ActivityKind.Internal);

        if (activity is null)
        {
            return await next();
        }

        // Add standard tags
        activity.SetTag("messaging.system", "mediatr");
        activity.SetTag("messaging.operation", operationType);
        activity.SetTag("messaging.destination", requestName);
        activity.SetTag("request.type", requestType.FullName);

        // Add request-specific tags from properties (if they exist)
        EnrichWithRequestProperties(activity, request);

        try
        {
            var result = await next();

            // Set result status
            if (result.IsFailure)
            {
                activity.SetStatus(ActivityStatusCode.Error, result.Error.Message);
                activity.SetTag("result.success", false);
                activity.SetTag("result.error.code", result.Error.Code);
                activity.SetTag("result.error.message", result.Error.Message);
            }
            else
            {
                activity.SetStatus(ActivityStatusCode.Ok);
                activity.SetTag("result.success", true);
            }

            return result;
        }
        catch (Exception ex)
        {
            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity.SetTag("exception.type", ex.GetType().FullName);
            activity.SetTag("exception.message", ex.Message);
            activity.SetTag("exception.stacktrace", ex.StackTrace);
            throw;
        }
    }

    private static void EnrichWithRequestProperties(Activity activity, TRequest request)
    {
        var properties = typeof(TRequest).GetProperties();

        foreach (var property in properties)
        {
            var value = property.GetValue(request);
            if (value is null) continue;

            var propertyName = property.Name.ToLowerInvariant();

            // Only add known safe properties (avoid sensitive data)
            if (IsSafeProperty(propertyName))
            {
                var tagName = $"request.{propertyName}";

                // Handle common types
                switch (value)
                {
                    case string str:
                        activity.SetTag(tagName, str);
                        break;
                    case Guid guid:
                        activity.SetTag(tagName, guid.ToString());
                        break;
                    case int intVal:
                        activity.SetTag(tagName, intVal);
                        break;
                    case long longVal:
                        activity.SetTag(tagName, longVal);
                        break;
                    case bool boolVal:
                        activity.SetTag(tagName, boolVal);
                        break;
                    case Enum enumVal:
                        activity.SetTag(tagName, enumVal.ToString());
                        break;
                }
            }
        }
    }

    private static bool IsSafeProperty(string propertyName)
    {
        // List of properties that are safe to include in traces
        var safeProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id",
            "tokenid",
            "poolid",
            "address",
            "tokenaddress",
            "pooladdress",
            "page",
            "pagesize",
            "skip",
            "take",
            "fromblock",
            "toblock",
            "chainid",
            "dex",
            "type",
            "status",
            "includetransactions"
        };

        // Exclude sensitive properties
        var sensitiveProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password",
            "secret",
            "token",
            "key",
            "apikey",
            "authorization",
            "credential"
        };

        return safeProperties.Contains(propertyName) &&
               !sensitiveProperties.Any(s => propertyName.Contains(s, StringComparison.OrdinalIgnoreCase));
    }
}
