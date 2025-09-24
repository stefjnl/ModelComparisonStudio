using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ModelComparisonStudio.Controllers;

/// <summary>
/// Base controller providing common error response functionality
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
    protected readonly ILogger _logger;

    protected BaseController(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a standardized error response for internal server errors
    /// </summary>
    protected static object CreateErrorResponse(Exception ex)
    {
        return new
        {
            type = "internal_error",
            title = "Internal Server Error",
            status = 500,
            detail = ex.Message,
            traceId = Guid.NewGuid().ToString(),
            userMessage = "An unexpected error occurred. Please try again later."
        };
    }

    /// <summary>
    /// Creates a standardized validation error response for a single error message
    /// </summary>
    protected static object CreateValidationErrorResponse(string message)
    {
        return new
        {
            type = "validation_error",
            title = "Validation Error",
            status = 400,
            detail = message,
            traceId = Guid.NewGuid().ToString(),
            userMessage = message
        };
    }

    /// <summary>
    /// Creates a standardized validation error response for model state errors
    /// </summary>
    protected static object CreateValidationErrorResponse(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
    {
        var errors = modelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        return new
        {
            type = "validation_error",
            title = "Validation Error",
            status = 400,
            errors = errors,
            traceId = Guid.NewGuid().ToString(),
            userMessage = string.Join(", ", errors)
        };
    }

    /// <summary>
    /// Creates a standardized validation error response for a list of error messages
    /// </summary>
    protected static object CreateValidationErrorResponse(List<string> errors)
    {
        return new
        {
            type = "validation_error",
            title = "Validation Error",
            status = 400,
            errors = errors,
            traceId = Guid.NewGuid().ToString(),
            userMessage = string.Join(", ", errors)
        };
    }
}