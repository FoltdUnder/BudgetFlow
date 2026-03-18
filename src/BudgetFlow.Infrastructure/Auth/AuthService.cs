using BudgetFlow.Application.Authentication.Models;
using BudgetFlow.Application.Authentication;
using BudgetFlow.Domain.Entities;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using BudgetFlow.Application.Common.Exceptions;
using System.Net.Mail;

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
        ValidateRegisterRequest(request);

        var normalizedEmail = request.Email.Trim();

        var isExists = await _dbContext.Users
            .AnyAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (isExists)
            throw new ValidationException("User with this email already exists.");

        var user = new User(
            request.FullName.Trim(),
            normalizedEmail
        );

        user.SetPasswordHash(_passwordHasher.HashPassword(user, request.Password));

        _dbContext.Users.Add(user);
        _dbContext.AuditLogs.Add(new AuditLog(
            user.Id,
            "user_registered",
            nameof(User),
            user.Id,
            $"User '{normalizedEmail}' registered."));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(string AccessToken, string RefreshToken)> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        ValidateLoginRequest(request);

        var normalizedEmail = request.Email.Trim();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            await AddAuditLogAsync(
                null,
                "login_failed",
                null,
                $"Login failed for '{normalizedEmail}': user not found.",
                cancellationToken);
            throw new UnauthorizedAccessException($"Invalid credentials. {request.Email}");
        }

        if (user.IsBlocked)
        {
            _logger.LogWarning("Blocked user {UserId} attempted to sign in.", user.Id);
            await AddAuditLogAsync(
                user.Id,
                "login_failed",
                user.Id,
                $"Login failed for '{normalizedEmail}': account is blocked.",
                cancellationToken);
            throw new UnauthorizedAccessException("This account is blocked.");
        }

        var passwordIsValid = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (passwordIsValid == PasswordVerificationResult.Failed || passwordIsValid == PasswordVerificationResult.SuccessRehashNeeded)
        {
            await AddAuditLogAsync(
                user.Id,
                "login_failed",
                user.Id,
                $"Login failed for '{normalizedEmail}': invalid password.",
                cancellationToken);
            throw new UnauthorizedAccessException("Invalid password.");
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken(
            user.Id,
            refreshTokenValue,
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays));

        _dbContext.RefreshTokens.Add(refreshToken);
        _dbContext.AuditLogs.Add(new AuditLog(
            user.Id,
            "login_succeeded",
            nameof(User),
            user.Id,
            $"User '{normalizedEmail}' signed in."));

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

    private static void ValidateRegisterRequest(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new ValidationException("Full name is required.");
        }

        ValidateEmail(request.Email);
        ValidatePassword(request.Password);
    }

    private static void ValidateLoginRequest(LoginRequest request)
    {
        ValidateEmail(request.Email);

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Password is required.");
        }
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required.");
        }

        try
        {
            _ = new MailAddress(email.Trim());
        }
        catch (FormatException)
        {
            throw new ValidationException("Email format is invalid.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Password is required.");
        }

        if (password.Length < 4)
        {
            throw new ValidationException("Password must be at least 4 characters long.");
        }
    }

    private async Task AddAuditLogAsync(
        Guid? userId,
        string action,
        Guid? entityId,
        string description,
        CancellationToken cancellationToken)
    {
        _dbContext.AuditLogs.Add(new AuditLog(
            userId,
            action,
            nameof(User),
            entityId,
            description));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
