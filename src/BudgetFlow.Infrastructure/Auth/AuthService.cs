using BudgetFlow.Application.Authentication.Models;
using BudgetFlow.Application.Authentication;
using BudgetFlow.Domain.Entities;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BudgetFlow.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext dbContext,
        ITokenService tokenService,
        IOptions<JwtOptions> jwtOptions,
        IPasswordHasher<User> passwordHasher,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var isExists = await _dbContext.Users
            .AnyAsync(x => x.Email == request.Email, cancellationToken);

        if (isExists)
            throw new InvalidOperationException("User with this email already exists.");

        var user = new User(
            request.FullName,
            request.Email
        );

        user.SetPasswordHash(_passwordHasher.HashPassword(user, request.Password));

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(string AccessToken, string RefreshToken)> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

        if (user is null)
            throw new UnauthorizedAccessException($"Invalid credentials. {request.Email}");

        if (user.IsBlocked)
        {
            _logger.LogWarning("Blocked user {UserId} attempted to sign in.", user.Id);
            throw new UnauthorizedAccessException("This account is blocked.");
        }

        var passwordIsValid = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (passwordIsValid == PasswordVerificationResult.Failed || passwordIsValid == PasswordVerificationResult.SuccessRehashNeeded)
            throw new UnauthorizedAccessException("Invalid password.");

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken(
            user.Id,
            refreshTokenValue,
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays));

        _dbContext.RefreshTokens.Add(refreshToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (accessToken, refreshTokenValue);
    }

    public async Task<(string AccessToken, string RefreshToken)> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var existingRefreshToken = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == refreshToken, cancellationToken);

        if (existingRefreshToken is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!existingRefreshToken.IsActive)
            throw new UnauthorizedAccessException("Refresh token is expired or revoked.");

        if (existingRefreshToken.User.IsBlocked)
        {
            existingRefreshToken.Revoke();
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("This account is blocked.");
        }

        existingRefreshToken.Revoke();

        var newAccessToken = _tokenService.GenerateAccessToken(existingRefreshToken.User);
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken(
            existingRefreshToken.UserId,
            newRefreshTokenValue,
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays));

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (newAccessToken, newRefreshTokenValue);
    }

    public async Task LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var existingRefreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken, cancellationToken);

        if (existingRefreshToken is null)
            return;

        existingRefreshToken.Revoke();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
