namespace AnalyzerCore.Api.Middleware;

/// <summary>
/// Middleware that enforces request body size limits.
/// </summary>
public class RequestSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestSizeLimitMiddleware> _logger;
    private readonly long _maxRequestBodySize;

    public RequestSizeLimitMiddleware(
        RequestDelegate next,
        ILogger<RequestSizeLimitMiddleware> logger,
        long maxRequestBodySize = 10 * 1024 * 1024) // Default 10MB
    {
        _next = next;
        _logger = logger;
        _maxRequestBodySize = maxRequestBodySize;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check Content-Length header
        if (context.Request.ContentLength.HasValue &&
            context.Request.ContentLength.Value > _maxRequestBodySize)
        {
            _logger.LogWarning(
                "Request rejected: Content-Length {ContentLength} exceeds limit {MaxSize}",
                context.Request.ContentLength.Value,
                _maxRequestBodySize);

            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Request body too large",
                maxSize = _maxRequestBodySize
            });
            return;
        }

        await _next(context);
    }
}
