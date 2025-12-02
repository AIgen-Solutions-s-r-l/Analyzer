namespace AnalyzerCore.Api.Middleware;

/// <summary>
/// Extension methods for adding middleware to the application pipeline.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds correlation ID middleware to the request pipeline.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
