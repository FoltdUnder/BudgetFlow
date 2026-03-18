using BudgetFlow.Application.Dashboard;
using BudgetFlow.Application.Transactions;
using BudgetFlow.Domain.Types;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetFlow.Infrastructure.Dashboard;

public sealed class UserDashboardService : IUserDashboardService
{
    private readonly AppDbContext _dbContext;

    public UserDashboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserDashboardDto> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = monthStart.AddMonths(1);

        var totalBalance = await _dbContext.Wallets
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (decimal?)x.Balance)
            .SumAsync(cancellationToken) ?? 0m;

        var monthTransactions = await _dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= monthStart && x.Date < nextMonthStart)
            .Select(x => new
            {
                x.Type,
                x.Amount
            })
            .ToListAsync(cancellationToken);

        var monthIncome = monthTransactions
            .Where(x => x.Type == CategoryType.Income)
            .Sum(x => x.Amount);

        var monthExpense = monthTransactions
            .Where(x => x.Type == CategoryType.Expense)
            .Sum(x => x.Amount);

        var latestTransactions = await _dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .Select(x => new TransactionDto(
                x.Id,
                x.UserId,
                x.WalletId,
                x.CategoryId,
                x.Type,
                x.Amount,
                x.Date,
                x.Note,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new UserDashboardDto(
            totalBalance,
            monthIncome,
            monthExpense,
            monthIncome - monthExpense,
            latestTransactions);
    }
}
