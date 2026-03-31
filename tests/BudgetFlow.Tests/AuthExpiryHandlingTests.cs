using System.Text.Json;
using BudgetFlow.Api.Common;
using BudgetFlow.Application.Authentication;
using BudgetFlow.Application.Common.Exceptions;
using BudgetFlow.Domain.Entities;
using BudgetFlow.Infrastructure.Auth;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BudgetFlow.Tests;

public sealed class AuthExpiryHandlingTests
{
    [Fact]
    public async Task RefreshAsync_WithExpiredRefreshToken_ThrowsExpiredRefreshTokenException()
    {
        await using var dbContext = CreateDbContext();
        var user = new User("Alice Example", "alice@example.com");
        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(new RefreshToken(
            user.Id,
            "expired-refresh-token",
            DateTime.UtcNow.AddMinutes(-5)));
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext);

        var action = () => service.RefreshAsync("expired-refresh-token", CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ExpiredRefreshTokenException>(action);
        Assert.Equal("Refresh token has expired.", exception.Message);
    }

    [Fact]
    public async Task RefreshAsync_WithRevokedRefreshToken_ThrowsUnauthorizedAccessException()
    {
        await using var dbContext = CreateDbContext();
        var user = new User("Alice Example", "alice@example.com");
        var refreshToken = new RefreshToken(
            user.Id,
            "revoked-refresh-token",
            DateTime.UtcNow.AddDays(1));
        refreshToken.Revoke();

        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext);

        var action = () => service.RefreshAsync("revoked-refresh-token", CancellationToken.None);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
        Assert.Equal("Refresh token is revoked.", exception.Message);
    }

    [Fact]
    public async Task RefreshAsync_WithMissingRefreshToken_ThrowsUnauthorizedAccessException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateAuthService(dbContext);

        var action = () => service.RefreshAsync("missing-refresh-token", CancellationToken.None);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
        Assert.Equal("Invalid refresh token.", exception.Message);
    }

    [Fact]
    public async Task HandleChallengeAsync_WithExpiredAccessToken_WritesAccessTokenExpiredCode()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Items[JwtAuthErrorResponseWriter.AccessTokenExpiredKey] = true;

        var context = CreateJwtBearerChallengeContext(httpContext);

        await JwtAuthErrorResponseWriter.HandleChallengeAsync(context);

        httpContext.Response.Body.Position = 0;
        var payload = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(httpContext.Response.Body);

        Assert.NotNull(payload);
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        Assert.Equal("application/json", httpContext.Response.ContentType);
        Assert.Equal("access_token_expired", payload.Code);
        Assert.Equal("Access token has expired.", payload.Message);
        Assert.Equal(StatusCodes.Status401Unauthorized, payload.Status);
    }

    [Fact]
    public async Task HandleChallengeAsync_WithInvalidAccessToken_WritesUnauthorizedCode()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var context = CreateJwtBearerChallengeContext(httpContext);

        await JwtAuthErrorResponseWriter.HandleChallengeAsync(context);

        httpContext.Response.Body.Position = 0;
        var payload = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(httpContext.Response.Body);

        Assert.NotNull(payload);
        Assert.Equal("unauthorized", payload.Code);
        Assert.Equal("Authentication is required.", payload.Message);
        Assert.Equal(StatusCodes.Status401Unauthorized, payload.Status);
    }

    [Fact]
    public async Task HandleAuthenticationFailedAsync_WithExpiredToken_MarksHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(
            JwtBearerDefaults.AuthenticationScheme,
            JwtBearerDefaults.AuthenticationScheme,
            typeof(JwtBearerHandler));
        var options = new JwtBearerOptions();
        var exception = new SecurityTokenExpiredException("expired");
        var context = new AuthenticationFailedContext(httpContext, scheme, options)
        {
            Exception = exception
        };

        await JwtAuthErrorResponseWriter.HandleAuthenticationFailedAsync(context);

        Assert.True(httpContext.Items.TryGetValue(JwtAuthErrorResponseWriter.AccessTokenExpiredKey, out var value));
        Assert.Equal(true, value);
    }

    private static JwtBearerChallengeContext CreateJwtBearerChallengeContext(HttpContext httpContext)
    {
        var scheme = new AuthenticationScheme(
            JwtBearerDefaults.AuthenticationScheme,
            JwtBearerDefaults.AuthenticationScheme,
            typeof(JwtBearerHandler));
        var options = new JwtBearerOptions();
        var properties = new AuthenticationProperties();

        return new JwtBearerChallengeContext(httpContext, scheme, options, properties);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AuthService CreateAuthService(AppDbContext dbContext)
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Key = "super-secret-key-that-is-long-enough-for-hmac-signing-12345",
            Issuer = "BudgetFlow",
            Audience = "BudgetFlow.Client",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });

        var passwordHasher = new PasswordHasher<User>();
        var tokenService = new TokenService(jwtOptions);

        return new AuthService(
            dbContext,
            tokenService,
            jwtOptions,
            passwordHasher,
            NullLogger<AuthService>.Instance);
    }
}
