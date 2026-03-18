namespace BudgetFlow.Application.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken);
    Task ChangeRoleAsync(Guid userId, string role, CancellationToken cancellationToken);
    Task BlockAsync(Guid userId, CancellationToken cancellationToken);
    Task UnblockAsync(Guid userId, CancellationToken cancellationToken);
}
