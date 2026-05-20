using System;
using System.Net;
using System.Text.Json;

namespace GearNXT_Backend.Middleware;

public class ExceptionMiddleware
{
private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // proceed normally
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        // Map exception type to HTTP status code
        var (statusCode, message) = ex switch
        {
            ArgumentException =>
                (HttpStatusCode.BadRequest, ex.Message),

            UnauthorizedAccessException =>
                (HttpStatusCode.Forbidden, ex.Message),

            KeyNotFoundException =>
                (HttpStatusCode.NotFound, ex.Message),

            InvalidOperationException =>
                (HttpStatusCode.Conflict, ex.Message),

            _ =>
                (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            success = false,
            statusCode = (int)statusCode,
            message
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
