using System.Diagnostics;
using System.Text.RegularExpressions;
using Serilog.Context;

namespace AnalyzerCore.Api.Middleware;

/// <summary>
/// Middleware that logs HTTP requests and responses with sensitive data masking.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    // Patterns for sensitive data that should be masked
    private static readonly Regex PasswordPattern = new(
        @"(password|pwd|secret|token|apikey|api_key|authorization)([""':=\s]+)([^\s""',}]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BearerTokenPattern = new(
        @"Bearer\s+[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+",
        RegexOptions.Compiled);

    private static readonly string[] SensitiveHeaders = new[]
    {
        "Authorization",
        "X-API-Key",
        "Cookie",
        "Set-Cookie"
    };

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;

        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            LogRequest(context);

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                LogResponse(context, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    private void LogRequest(HttpContext context)
    {
        var request = context.Request;
        var maskedHeaders = MaskSensitiveHeaders(request.Headers);

        _logger.LogInformation(
            "HTTP {Method} {Path}{QueryString} started - Headers: {Headers}",
            request.Method,
            request.Path,
            MaskQueryString(request.QueryString.ToString()),
            maskedHeaders);
    }

    private void LogResponse(HttpContext context, long elapsedMs)
    {
        var statusCode = context.Response.StatusCode;
        var level = statusCode >= 500 ? LogLevel.Error :
                    statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

        _logger.Log(
            level,
            "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms",
            context.Request.Method,
            context.Request.Path,
            statusCode,
            elapsedMs);
    }

    private static Dictionary<string, string> MaskSensitiveHeaders(IHeaderDictionary headers)
    {
        var maskedHeaders = new Dictionary<string, string>();

        foreach (var header in headers)
        {
            if (SensitiveHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            {
                maskedHeaders[header.Key] = "[REDACTED]";
            }
            else
            {
                maskedHeaders[header.Key] = header.Value.ToString();
            }
        }

        return maskedHeaders;
    }

    private static string MaskQueryString(string queryString)
    {
        if (string.IsNullOrEmpty(queryString))
            return queryString;

        return PasswordPattern.Replace(queryString, "$1$2[REDACTED]");
    }

    /// <summary>
    /// Masks sensitive data in a string.
    /// </summary>
    public static string MaskSensitiveData(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        var result = PasswordPattern.Replace(input, "$1$2[REDACTED]");
        result = BearerTokenPattern.Replace(result, "Bearer [REDACTED]");

        return result;
    }
}
