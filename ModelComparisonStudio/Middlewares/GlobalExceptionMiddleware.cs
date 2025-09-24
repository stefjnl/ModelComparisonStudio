using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ModelComparisonStudio.Core.Exceptions;

namespace ModelComparisonStudio.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = new
            {
                StatusCode = GetStatusCode(exception),
                Message = GetMessage(exception),
                Details = GetDetails(exception)
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = response.StatusCode;

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private static int GetStatusCode(Exception exception)
        {
            return exception switch
            {
                ValidationException => StatusCodes.Status400BadRequest,
                AuthenticationException => StatusCodes.Status401Unauthorized,
                NotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };
        }

        private static string GetMessage(Exception exception)
        {
            return exception switch
            {
                ValidationException => "Validation error occurred.",
                AuthenticationException => "Authentication error occurred.",
                NotFoundException => "The requested resource was not found.",
                _ => "An internal server error occurred."
            };
        }

        private static string GetDetails(Exception exception)
        {
#if DEBUG
            return exception.Message + Environment.NewLine + exception.StackTrace;
#else
            return exception.Message;
#endif
        }
    }
}