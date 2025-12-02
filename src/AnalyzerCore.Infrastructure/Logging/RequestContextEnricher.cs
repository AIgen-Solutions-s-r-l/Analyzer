using AnalyzerCore.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace AnalyzerCore.Infrastructure.Logging;

/// <summary>
/// Enriches log events with HTTP request context.
/// </summary>
public sealed class RequestContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestContextEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return;

        // Add request path
        if (httpContext.Request.Path.HasValue)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "RequestPath",
                httpContext.Request.Path.Value));
        }

        // Add HTTP method
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
            "RequestMethod",
            httpContext.Request.Method));

        // Add correlation ID from header or items
        var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                         ?? httpContext.Items["CorrelationId"]?.ToString();
        if (!string.IsNullOrEmpty(correlationId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "CorrelationId",
                correlationId));
        }

        // Add trace ID if available
        var traceId = httpContext.TraceIdentifier;
        if (!string.IsNullOrEmpty(traceId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "TraceId",
                traceId));
        }

        // Add user identity if authenticated
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "UserId",
                httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.FindFirst("nameid")?.Value
                    ?? httpContext.User.Identity.Name
                    ?? "Unknown"));
        }

        // Add client IP
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(clientIp))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "ClientIp",
                clientIp));
        }

        // Add user agent
        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        if (!string.IsNullOrEmpty(userAgent))
        {
            // Truncate user agent to prevent huge log entries
            if (userAgent.Length > 200)
                userAgent = userAgent[..200] + "...";

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "UserAgent",
                userAgent));
        }
    }
}
