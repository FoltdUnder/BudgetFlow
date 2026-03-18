namespace BudgetFlow.Application.Dashboard;

public interface IUserDashboardService
{
    Task<UserDashboardDto> GetAsync(Guid userId, CancellationToken cancellationToken);
}
