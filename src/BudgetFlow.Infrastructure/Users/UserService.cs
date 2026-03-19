using BudgetFlow.Application.Common.Exceptions;
using BudgetFlow.Application.Users;
using BudgetFlow.Domain.Entities;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetFlow.Infrastructure.Users;

public sealed class UserService : IUserService
{
    private readonly AppDbContext _dbContext;

    public UserService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new UserDto(
                x.Id,
                x.FullName,
                x.Email,
                x.Role,
                x.IsBlocked,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task ChangeRoleAsync(Guid actorUserId, Guid userId, string role, CancellationToken cancellationToken)
    {
        if (role is not Roles.User && role is not Roles.Admin)
        {
            throw new ValidationException($"Role must be either '{Roles.User}' or '{Roles.Admin}'.");
        }

        var user = await GetUserAsync(userId, cancellationToken);

        user.ChangeRole(role);
        _dbContext.AuditLogs.Add(new AuditLog(
            actorUserId,
            "user_role_changed",
            nameof(User),
            user.Id,
            $"User role changed to '{role}'."));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BlockAsync(Guid actorUserId, Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);

        user.Block();
        _dbContext.AuditLogs.Add(new AuditLog(
            actorUserId,
            "user_blocked",
            nameof(User),
            user.Id,
            $"User '{user.Email}' was blocked."));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UnblockAsync(Guid actorUserId, Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);

        user.Unblock();
        _dbContext.AuditLogs.Add(new AuditLog(
            actorUserId,
            "user_unblocked",
            nameof(User),
            user.Id,
            $"User '{user.Email}' was unblocked."));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException($"User '{userId}' was not found.");
        }

        return user;
    }
}
