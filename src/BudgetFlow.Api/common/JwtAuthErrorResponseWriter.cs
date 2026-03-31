using System.Text.Json;
using BudgetFlow.Api.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace BudgetFlow.Api.Common;

public static class JwtAuthErrorResponseWriter
{
    public const string AccessTokenExpiredKey = "auth.access_token_expired";

    public static Task HandleAuthenticationFailedAsync(AuthenticationFailedContext context)
    {
        if (context.Exception is SecurityTokenExpiredException)
        {
            context.HttpContext.Items[AccessTokenExpiredKey] = true;
        }

        return Task.CompletedTask;
    }

    public static async Task HandleChallengeAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();

        var accessTokenExpired = context.HttpContext.Items.TryGetValue(AccessTokenExpiredKey, out var value)
            && value is true;

        var response = new ApiErrorResponse
        {
            Message = accessTokenExpired
                ? "Access token has expired."
                : "Authentication is required.",
            Code = accessTokenExpired
                ? "access_token_expired"
                : "unauthorized",
            Status = StatusCodes.Status401Unauthorized
        };

        context.Response.StatusCode = response.Status;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
