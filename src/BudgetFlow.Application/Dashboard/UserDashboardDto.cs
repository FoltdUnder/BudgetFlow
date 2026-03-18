using BudgetFlow.Application.Transactions;

namespace BudgetFlow.Application.Dashboard;

public sealed record UserDashboardDto(
    decimal TotalBalance,
    decimal MonthIncome,
    decimal MonthExpense,
    decimal MonthNet,
    IReadOnlyList<TransactionDto> LatestTransactions);
