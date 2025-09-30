using FluentValidation;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Orders.API.Middleware;

/// <summary>
/// Global exception handling middleware for Orders API
/// Provides consistent error responses and logging for unhandled exceptions
/// </summary>
/// <param name="next">Next middleware in the pipeline</param>
/// <param name="logger">Logger for diagnostic information</param>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    /// <summary>
    /// Middleware invocation method
    /// </summary>
    /// <param name="context">HTTP context</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles different types of exceptions and returns appropriate HTTP responses
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="exception">Exception to handle</param>
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = context.Response;
        var errorResponse = new ErrorResponse();

        switch (exception)
        {
            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Title = "Validation Error";
                errorResponse.Status = (int)HttpStatusCode.BadRequest;
                errorResponse.Detail = "One or more validation errors occurred.";
                errorResponse.Errors = validationEx.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.ErrorMessage).ToArray()
                    );
                break;

            case ArgumentException argEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Title = "Bad Request";
                errorResponse.Status = (int)HttpStatusCode.BadRequest;
                errorResponse.Detail = argEx.Message;
                break;

            case InvalidOperationException invalidOpEx:
                response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                errorResponse.Title = "Business Rule Violation";
                errorResponse.Status = (int)HttpStatusCode.UnprocessableEntity;
                errorResponse.Detail = invalidOpEx.Message;
                break;

            case KeyNotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Title = "Resource Not Found";
                errorResponse.Status = (int)HttpStatusCode.NotFound;
                errorResponse.Detail = notFoundEx.Message;
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Title = "Unauthorized";
                errorResponse.Status = (int)HttpStatusCode.Unauthorized;
                errorResponse.Detail = "You are not authorized to perform this action.";
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Title = "Internal Server Error";
                errorResponse.Status = (int)HttpStatusCode.InternalServerError;
                errorResponse.Detail = "An unexpected error occurred. Please try again later.";
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Standard error response model following RFC 7807 (Problem Details)
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// A short, human-readable summary of the problem type
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The HTTP status code
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// Validation errors (if applicable)
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    /// Trace identifier for correlation with logs
    /// </summary>
    public string TraceId { get; set; } = Activity.Current?.Id ?? string.Empty;

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}