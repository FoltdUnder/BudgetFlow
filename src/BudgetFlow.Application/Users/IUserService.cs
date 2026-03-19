namespace BudgetFlow.Application.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken);
    Task ChangeRoleAsync(Guid actorUserId, Guid userId, string role, CancellationToken cancellationToken);
    Task BlockAsync(Guid actorUserId, Guid userId, CancellationToken cancellationToken);
    Task UnblockAsync(Guid actorUserId, Guid userId, CancellationToken cancellationToken);
}
