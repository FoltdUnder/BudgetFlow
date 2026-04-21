using BudgetFlow.Application.Authentication.Models;
using BudgetFlow.Application.Common.Exceptions;
using BudgetFlow.Domain.Entities;
using BudgetFlow.Infrastructure.Auth;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BudgetFlow.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesUser_AuditLogs_AndRefreshToken()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<User>();
        var service = CreateAuthService(dbContext, passwordHasher);
        var request = new RegisterRequest("alice@example.com", "password", "Alice Example");

        var result = await service.RegisterAsync(request, CancellationToken.None);

        var user = await dbContext.Users.SingleAsync();
        var refreshToken = await dbContext.RefreshTokens.SingleAsync();
        var auditLogs = await dbContext.AuditLogs.OrderBy(x => x.CreatedAtUtc).ToListAsync();

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal(refreshToken.Token, result.RefreshToken);
        Assert.Equal("Alice Example", user.FullName);
        Assert.Equal("alice@example.com", user.Email);
        Assert.NotEqual("password", user.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, "password"));
        Assert.Equal(user.Id, refreshToken.UserId);
        Assert.False(refreshToken.IsRevoked);
        Assert.Equal(2, auditLogs.Count);
        Assert.Equal("user_registered", auditLogs[0].Action);
        Assert.Equal("login_succeeded", auditLogs[1].Action);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User("Alice Example", "alice@example.com"));
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext);

        var action = () => service.RegisterAsync(
            new RegisterRequest("alice@example.com", "password", "Alice Example"),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ValidationException>(action);
        Assert.Equal("User with this email already exists.", exception.Message);
    }

    [Theory]
    [InlineData("", "alice@example.com", "password", "Full name is required.")]
    [InlineData("Alice Example", "not-an-email", "password", "Email format is invalid.")]
    [InlineData("Alice Example", "alice@example.com", "", "Password is required.")]
    [InlineData("Alice Example", "alice@example.com", "123", "Password must be at least 4 characters long.")]
    public async Task RegisterAsync_WithInvalidInput_ThrowsValidationException(
        string fullName,
        string email,
        string password,
        string expectedMessage)
    {
        await using var dbContext = CreateDbContext();
        var service = CreateAuthService(dbContext);

        var action = () => service.RegisterAsync(
            new RegisterRequest(email, password, fullName),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ValidationException>(action);
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithMissingUser_AddsFailedAuditLog_AndThrowsUnauthorizedAccessException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateAuthService(dbContext);

        var action = () => service.LoginAsync(
            new LoginRequest("missing@example.com", "password"),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
        var auditLog = await dbContext.AuditLogs.SingleAsync();

        Assert.Equal("Invalid credentials. missing@example.com", exception.Message);
        Assert.Null(auditLog.UserId);
        Assert.Equal("login_failed", auditLog.Action);
        Assert.Contains("user not found", auditLog.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginAsync_WithBlockedUser_AddsFailedAuditLog_AndThrowsUnauthorizedAccessException()
    {
        await using var dbContext = CreateDbContext();
        var user = new User("Alice Example", "alice@example.com");
        user.Block();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext);

        var action = () => service.LoginAsync(
            new LoginRequest("alice@example.com", "password"),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
        var auditLog = await dbContext.AuditLogs.SingleAsync();

        Assert.Equal("This account is blocked.", exception.Message);
        Assert.Equal(user.Id, auditLog.UserId);
        Assert.Equal("login_failed", auditLog.Action);
        Assert.Contains("account is blocked", auditLog.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_AddsFailedAuditLog_AndThrowsUnauthorizedAccessException()
    {
        await using var dbContext = CreateDbContext();
        var user = new User("Alice Example", "alice@example.com");
        var passwordHasher = new PasswordHasher<User>();
        user.SetPasswordHash(passwordHasher.HashPassword(user, "correct-password"));
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext, passwordHasher);

        var action = () => service.LoginAsync(
            new LoginRequest("alice@example.com", "wrong-password"),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
        var auditLog = await dbContext.AuditLogs.SingleAsync();

        Assert.Equal("Invalid password.", exception.Message);
        Assert.Equal(user.Id, auditLog.UserId);
        Assert.Equal("login_failed", auditLog.Action);
        Assert.Contains("invalid password", auditLog.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginAsync_WithPasswordRehashNeeded_UpdatesPasswordHash_AndReturnsTokens()
    {
        await using var dbContext = CreateDbContext();
        var user = new User("Alice Example", "alice@example.com");
        user.SetPasswordHash("legacy-password");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var passwordHasher = new RehashingPasswordHasher();
        var service = CreateAuthService(dbContext, passwordHasher);

        var result = await service.LoginAsync(
            new LoginRequest("alice@example.com", "password"),
            CancellationToken.None);

        var storedUser = await dbContext.Users.SingleAsync();
        var refreshToken = await dbContext.RefreshTokens.SingleAsync();

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal(refreshToken.Token, result.RefreshToken);
        Assert.Equal("rehashed-password", storedUser.PasswordHash);
        Assert.Equal(1, await dbContext.AuditLogs.CountAsync(x => x.Action == "login_succeeded"));
    }

    [Fact]
    public async Task RefreshAsync_WithValidRefreshToken_RevokesOldToken_StoresNewToken_AndReturnsTokens()
    {
        await using var dbContext = CreateDbContext();
        var user = new User("Alice Example", "alice@example.com");
        var existingRefreshToken = new RefreshToken(user.Id, "refresh-token", DateTime.UtcNow.AddDays(1));
        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(existingRefreshToken);
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext);

        var result = await service.RefreshAsync("refresh-token", CancellationToken.None);

        var refreshTokens = await dbContext.RefreshTokens.OrderBy(x => x.CreatedAtUtc).ToListAsync();

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal(2, refreshTokens.Count);
        Assert.True(refreshTokens[0].IsRevoked);
        Assert.False(refreshTokens[1].IsRevoked);
        Assert.Equal(result.RefreshToken, refreshTokens[1].Token);
        Assert.NotEqual("refresh-token", result.RefreshToken);
    }

    [Fact]
    public async Task RefreshAsync_WithBlockedUser_RevokesExistingToken_AndThrowsUnauthorizedAccessException()
    {
        await using var dbContext = CreateDbContext();
        var user = new User("Alice Example", "alice@example.com");
        user.Block();
        var existingRefreshToken = new RefreshToken(user.Id, "refresh-token", DateTime.UtcNow.AddDays(1));
        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(existingRefreshToken);
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext);

        var action = () => service.RefreshAsync("refresh-token", CancellationToken.None);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
        var refreshTokens = await dbContext.RefreshTokens.ToListAsync();

        Assert.Equal("This account is blocked.", exception.Message);
        Assert.Single(refreshTokens);
        Assert.True(refreshTokens[0].IsRevoked);
    }

    [Fact]
    public async Task LogoutAsync_WithExistingRefreshToken_RevokesToken()
    {
        await using var dbContext = CreateDbContext();
        var user = new User("Alice Example", "alice@example.com");
        var refreshToken = new RefreshToken(user.Id, "refresh-token", DateTime.UtcNow.AddDays(1));
        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext);

        await service.LogoutAsync("refresh-token", CancellationToken.None);

        var storedRefreshToken = await dbContext.RefreshTokens.SingleAsync();
        Assert.True(storedRefreshToken.IsRevoked);
    }

    [Fact]
    public async Task LogoutAsync_WithMissingRefreshToken_DoesNothing()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateAuthService(dbContext);

        await service.LogoutAsync("missing-token", CancellationToken.None);

        Assert.Empty(await dbContext.RefreshTokens.ToListAsync());
        Assert.Empty(await dbContext.AuditLogs.ToListAsync());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AuthService CreateAuthService(
        AppDbContext dbContext,
        IPasswordHasher<User>? passwordHasher = null)
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Key = "super-secret-key-that-is-long-enough-for-hmac-signing-12345",
            Issuer = "BudgetFlow",
            Audience = "BudgetFlow.Client",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });

        return new AuthService(
            dbContext,
            new TokenService(jwtOptions),
            jwtOptions,
            passwordHasher ?? new PasswordHasher<User>(),
            NullLogger<AuthService>.Instance);
    }

    private sealed class RehashingPasswordHasher : IPasswordHasher<User>
    {
        public string HashPassword(User user, string password)
        {
            return $"rehashed-{password}";
        }

        public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
        {
            return hashedPassword == $"legacy-{providedPassword}"
                ? PasswordVerificationResult.SuccessRehashNeeded
                : PasswordVerificationResult.Failed;
        }
    }
}
