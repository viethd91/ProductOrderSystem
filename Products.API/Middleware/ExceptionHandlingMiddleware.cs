using FluentValidation;
using System.Net;
using System.Text.Json;

namespace Products.API.Middleware;

/// <summary>
/// Global exception handling middleware
/// Catches and handles all unhandled exceptions in the application
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                Title = "Validation Error",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = validationEx.Message,
                Errors = validationEx.Errors?.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            },
            InvalidOperationException invalidOpEx when invalidOpEx.Message.Contains("already exists") => new ErrorResponse
            {
                Title = "Conflict Error",
                Status = (int)HttpStatusCode.Conflict,
                Detail = invalidOpEx.Message
            },
            InvalidOperationException invalidOpEx when invalidOpEx.Message.Contains("not found") => new ErrorResponse
            {
                Title = "Not Found",
                Status = (int)HttpStatusCode.NotFound,
                Detail = invalidOpEx.Message
            },
            ArgumentException argEx => new ErrorResponse
            {
                Title = "Bad Request",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = argEx.Message
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                Title = "Unauthorized",
                Status = (int)HttpStatusCode.Unauthorized,
                Detail = "You are not authorized to perform this action"
            },
            _ => new ErrorResponse
            {
                Title = "Internal Server Error",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = "An internal server error occurred"
            }
        };

        response.StatusCode = errorResponse.Status;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Standard error response format following RFC 7807 Problem Details
/// </summary>
public class ErrorResponse
{
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string? Instance { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}