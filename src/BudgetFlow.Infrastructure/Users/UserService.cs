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

    public async Task ChangeRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        if (role is not Roles.User && role is not Roles.Admin)
        {
            throw new ValidationException($"Role must be either '{Roles.User}' or '{Roles.Admin}'.");
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException($"User '{userId}' was not found.");
        }

        user.ChangeRole(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
