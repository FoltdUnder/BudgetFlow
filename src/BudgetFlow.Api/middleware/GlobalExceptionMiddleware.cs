using System.Text.Json;
using BudgetFlow.Api.Common;
using BudgetFlow.Application.Common.Exceptions;

namespace BudgetFlow.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception occurred while processing request {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await HandleExceptionAsync(context, exception);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var error = MapException(exception);

        var response = new ApiErrorResponse
        {
            Message = error.Message,
            Code = error.Code,
            Status = error.Status
        };

        context.Response.StatusCode = error.Status;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }

    private static (int Status, string Code, string Message) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "validation_error",
                exception.Message),
            ArgumentOutOfRangeException => (
                StatusCodes.Status400BadRequest,
                "business_rule_violation",
                exception.Message),
            NotFoundException => (
                StatusCodes.Status404NotFound,
                "not_found",
                exception.Message),
            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "business_rule_conflict",
                exception.Message),
            ExpiredRefreshTokenException => (
                StatusCodes.Status401Unauthorized,
                "refresh_token_expired",
                exception.Message),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "unauthorized",
                exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "internal_server_error",
                "An unexpected error occurred.")
        };
    }
}
