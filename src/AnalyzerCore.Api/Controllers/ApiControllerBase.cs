using AnalyzerCore.Domain.Abstractions;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzerCore.Api.Controllers;

/// <summary>
/// Base API controller with Result pattern to ActionResult mapping.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Converts a Result to an appropriate ActionResult.
    /// </summary>
    protected IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok();
        }

        return ToErrorResult(result.Error);
    }

    /// <summary>
    /// Converts a Result&lt;T&gt; to an appropriate ActionResult.
    /// </summary>
    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return ToErrorResult(result.Error);
    }

    /// <summary>
    /// Converts a Result&lt;T&gt; to an appropriate ActionResult with 201 Created status.
    /// </summary>
    protected IActionResult ToCreatedResult<T>(Result<T> result, string? actionName = null, object? routeValues = null)
    {
        if (result.IsSuccess)
        {
            if (actionName is not null)
            {
                return CreatedAtAction(actionName, routeValues, result.Value);
            }
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return ToErrorResult(result.Error);
    }

    /// <summary>
    /// Maps domain errors to HTTP status codes and ProblemDetails.
    /// </summary>
    private IActionResult ToErrorResult(Error error)
    {
        var statusCode = GetStatusCode(error);

        return Problem(
            statusCode: statusCode,
            title: GetTitle(statusCode),
            type: GetType(statusCode),
            detail: error.Message,
            instance: HttpContext.Request.Path);
    }

    private static int GetStatusCode(Error error) => error.Code switch
    {
        // Not Found errors
        var code when code.EndsWith(".NotFound") => StatusCodes.Status404NotFound,

        // Already Exists errors (conflict)
        var code when code.EndsWith(".AlreadyExists") => StatusCodes.Status409Conflict,

        // Validation errors
        "Validation.Error" => StatusCodes.Status400BadRequest,
        var code when code.Contains(".Invalid") => StatusCodes.Status400BadRequest,
        var code when code.Contains(".NullOrEmpty") => StatusCodes.Status400BadRequest,

        // Null value
        "Error.NullValue" => StatusCodes.Status400BadRequest,

        // Blockchain/external service errors
        var code when code.StartsWith("Blockchain.") => StatusCodes.Status502BadGateway,

        // Default to 400 Bad Request
        _ => StatusCodes.Status400BadRequest
    };

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status502BadGateway => "Bad Gateway",
        _ => "An error occurred"
    };

    private static string GetType(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        StatusCodes.Status502BadGateway => "https://tools.ietf.org/html/rfc7231#section-6.6.3",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.5.1"
    };
}
