using BudgetFlow.Application.Authentication.Models;
using BudgetFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using BudgetFlow.Api.Contracts.Auth;

namespace BudgetFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(
            new LoginRequest(request.Email, request.Password),
            cancellationToken);

        return Ok(new AuthResponse(result.AccessToken, result.RefreshToken));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(
            request.RefreshToken,
            cancellationToken);

        return Ok(new AuthResponse(result.AccessToken, result.RefreshToken));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        await authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }
}