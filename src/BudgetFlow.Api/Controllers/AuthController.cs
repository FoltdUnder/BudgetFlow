using BudgetFlow.Application.Authentication.Models;
using BudgetFlow.Application.Authentication;
using BudgetFlow.Application.AuditLogs;
using BudgetFlow.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using BudgetFlow.Api.Common;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BudgetFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly RefreshTokenCookieManager _refreshTokenCookieManager;

    public AuthController(
        IAuthService authService,
        RefreshTokenCookieManager refreshTokenCookieManager)
    {
        _authService = authService;
        _refreshTokenCookieManager = refreshTokenCookieManager;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(
            new LoginRequest(request.Email, request.Password),
            cancellationToken);

        _refreshTokenCookieManager.AppendRefreshTokenCookie(HttpContext, result.RefreshToken);

        return Ok(new AuthResponse(result.AccessToken));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        CancellationToken cancellationToken)
    {
        var refreshToken = GetRefreshTokenFromCookie();
        var result = await _authService.RefreshAsync(
            refreshToken,
            cancellationToken);

        _refreshTokenCookieManager.AppendRefreshTokenCookie(HttpContext, result.RefreshToken);

        return Ok(new AuthResponse(result.AccessToken));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        CancellationToken cancellationToken)
    {
        if (_refreshTokenCookieManager.TryGetRefreshToken(Request, out var refreshToken))
        {
            await _authService.LogoutAsync(refreshToken, cancellationToken);
        }

        _refreshTokenCookieManager.DeleteRefreshTokenCookie(HttpContext);
        return NoContent();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        _refreshTokenCookieManager.AppendRefreshTokenCookie(HttpContext, result.RefreshToken);

        return StatusCode(StatusCodes.Status201Created, new AuthResponse(result.AccessToken));
    }

    [HttpGet("me")]
    [Authorize(Policy = "UserOnly")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roles = User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray();

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        return Ok(new
        {
            userId,
            roles
        });
    }

    private string GetRefreshTokenFromCookie()
    {
        if (_refreshTokenCookieManager.TryGetRefreshToken(Request, out var refreshToken))
        {
            return refreshToken;
        }

        throw new UnauthorizedAccessException("Refresh token is missing.");
    }
}
