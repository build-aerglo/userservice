using Microsoft.AspNetCore.Mvc;

namespace UserService.Api.Controllers;

/// <summary>
/// Base controller that provides a consistent error response shape:
/// { "error": "message" } for all non-200 responses.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Extracts the first validation error from ModelState and returns
    /// a consistent { error: "message" } body for a 400 response.
    /// </summary>
    protected object ValidationError()
    {
        var message = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
            ?? "Invalid request.";

        return new { error = message };
    }
}
