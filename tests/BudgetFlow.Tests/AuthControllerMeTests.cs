using System.Security.Claims;
using System.Text.Json;
using BudgetFlow.Api.Common;
using BudgetFlow.Api.Controllers;
using BudgetFlow.Application.Authentication;
using BudgetFlow.Application.Authentication.Models;
using BudgetFlow.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BudgetFlow.Tests;

public sealed class AuthControllerMeTests
{
    [Fact]
    public async Task Me_WithoutNameIdentifierClaim_ReturnsUnauthorized()
    {
        var controller = CreateController([]);

        var result = await controller.Me(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Me_WithInvalidNameIdentifierClaim_ReturnsUnauthorized()
    {
        var controller = CreateController([new Claim(ClaimTypes.NameIdentifier, "not-a-guid")]);

        var result = await controller.Me(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Me_WithValidClaims_ReturnsUserIdAndRoles()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "User")
        ]);

        var result = await controller.Me(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = JsonSerializer.SerializeToElement(okResult.Value);

        Assert.Equal(userId, payload.GetProperty("userId").GetGuid());
        Assert.Equal(["Admin", "User"], payload.GetProperty("roles").EnumerateArray().Select(x => x.GetString()).ToArray());
    }

    private static AuthController CreateController(Claim[] claims)
    {
        var controller = new AuthController(new FakeAuthService(), CreateCookieManager())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            }
        };

        return controller;
    }

    private static RefreshTokenCookieManager CreateCookieManager()
    {
        return new RefreshTokenCookieManager(Options.Create(new JwtOptions
        {
            Key = "super-secret-key-that-is-long-enough-for-hmac-signing-12345",
            Issuer = "BudgetFlow",
            Audience = "BudgetFlow.Client",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        }));
    }

    private sealed class FakeAuthService : IAuthService
    {
        public Task<(string AccessToken, string RefreshToken)> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<(string AccessToken, string RefreshToken)> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<(string AccessToken, string RefreshToken)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
