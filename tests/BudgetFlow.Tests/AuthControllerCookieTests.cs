using BudgetFlow.Api.Common;
using BudgetFlow.Api.Controllers;
using BudgetFlow.Application.Authentication;
using BudgetFlow.Application.Authentication.Models;
using BudgetFlow.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BudgetFlow.Tests;

public sealed class AuthControllerCookieTests
{
    [Fact]
    public async Task Login_SetsRefreshTokenCookie_AndReturnsOnlyAccessToken()
    {
        var authService = new FakeAuthService
        {
            LoginResult = ("access-token", "refresh-token")
        };
        var controller = CreateController(authService);

        var result = await controller.Login(
            new LoginRequest("alice@example.com", "password"),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AuthResponse>(okResult.Value);

        Assert.Equal("access-token", payload.AccessToken);
        Assert.Contains("refreshToken=refresh-token", controller.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal);
        Assert.Contains("httponly", controller.Response.Headers.SetCookie.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_SetsRefreshTokenCookie_AndReturnsCreatedWithAccessTokenOnly()
    {
        var authService = new FakeAuthService
        {
            RegisterResult = ("new-access-token", "new-refresh-token")
        };
        var controller = CreateController(authService);

        var result = await controller.Register(
            new RegisterRequest("alice@example.com", "password", "Alice Example"),
            CancellationToken.None);

        var createdResult = Assert.IsType<ObjectResult>(result);
        var payload = Assert.IsType<AuthResponse>(createdResult.Value);

        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal("new-access-token", payload.AccessToken);
        Assert.Contains("refreshToken=new-refresh-token", controller.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Refresh_ReadsRefreshTokenFromCookie_AndRotatesCookie()
    {
        var authService = new FakeAuthService
        {
            RefreshResult = ("rotated-access-token", "rotated-refresh-token")
        };
        var controller = CreateController(authService);
        controller.ControllerContext.HttpContext.Request.Headers.Cookie = $"{RefreshTokenCookieManager.CookieName}=cookie-refresh-token";

        var result = await controller.Refresh(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AuthResponse>(okResult.Value);

        Assert.Equal("cookie-refresh-token", authService.LastRefreshToken);
        Assert.Equal("rotated-access-token", payload.AccessToken);
        Assert.Contains("refreshToken=rotated-refresh-token", controller.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Logout_WithoutCookie_StillClearsCookieAndReturnsNoContent()
    {
        var authService = new FakeAuthService();
        var controller = CreateController(authService);

        var result = await controller.Logout(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(authService.LastLogoutRefreshToken);
        Assert.Contains("refreshToken=", controller.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Logout_WithCookie_RevokesRefreshTokenAndClearsCookie()
    {
        var authService = new FakeAuthService();
        var controller = CreateController(authService);
        controller.ControllerContext.HttpContext.Request.Headers.Cookie = $"{RefreshTokenCookieManager.CookieName}=cookie-refresh-token";

        var result = await controller.Logout(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("cookie-refresh-token", authService.LastLogoutRefreshToken);
        Assert.Contains("refreshToken=", controller.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal);
    }

    private static AuthController CreateController(IAuthService authService)
    {
        var cookieManager = new RefreshTokenCookieManager(Options.Create(new JwtOptions
        {
            Key = "super-secret-key-that-is-long-enough-for-hmac-signing-12345",
            Issuer = "BudgetFlow",
            Audience = "BudgetFlow.Client",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        }));

        return new AuthController(authService, cookieManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private sealed class FakeAuthService : IAuthService
    {
        public (string AccessToken, string RefreshToken) LoginResult { get; set; } = ("", "");
        public (string AccessToken, string RefreshToken) RefreshResult { get; set; } = ("", "");
        public (string AccessToken, string RefreshToken) RegisterResult { get; set; } = ("", "");
        public string? LastRefreshToken { get; private set; }
        public string? LastLogoutRefreshToken { get; private set; }

        public Task<(string AccessToken, string RefreshToken)> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(LoginResult);
        }

        public Task<(string AccessToken, string RefreshToken)> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            LastRefreshToken = refreshToken;
            return Task.FromResult(RefreshResult);
        }

        public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
        {
            LastLogoutRefreshToken = refreshToken;
            return Task.CompletedTask;
        }

        public Task<(string AccessToken, string RefreshToken)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(RegisterResult);
        }
    }
}
