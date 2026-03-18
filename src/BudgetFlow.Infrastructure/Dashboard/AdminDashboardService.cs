using BudgetFlow.Application.Dashboard;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetFlow.Infrastructure.Dashboard;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly AppDbContext _dbContext;

    public AdminDashboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminDashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var totalUsers = await _dbContext.Users
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var blockedUsers = await _dbContext.Users
            .AsNoTracking()
            .CountAsync(x => x.IsBlocked, cancellationToken);

        var totalWallets = await _dbContext.Wallets
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var totalTransactions = await _dbContext.Transactions
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var totalBalance = await _dbContext.Wallets
            .AsNoTracking()
            .Select(x => (decimal?)x.Balance)
            .SumAsync(cancellationToken) ?? 0m;

        return new AdminDashboardDto(
            totalUsers,
            blockedUsers,
            totalWallets,
            totalTransactions,
            totalBalance);
    }
}
