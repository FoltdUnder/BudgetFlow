namespace BudgetFlow.Application.Dashboard;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetAsync(CancellationToken cancellationToken);
}
