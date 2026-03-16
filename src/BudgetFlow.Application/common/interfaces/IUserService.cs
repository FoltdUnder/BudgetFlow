using BudgetFlow.Application.Users;

namespace BudgetFlow.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken);
    Task ChangeRoleAsync(Guid userId, string role, CancellationToken cancellationToken);
}
