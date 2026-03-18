namespace BudgetFlow.Application.Dashboard;

public sealed record AdminDashboardDto(
    int TotalUsers,
    int BlockedUsers,
    int TotalWallets,
    int TotalTransactions,
    decimal TotalBalance);
