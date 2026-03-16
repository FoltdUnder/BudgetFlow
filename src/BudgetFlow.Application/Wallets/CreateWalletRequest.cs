namespace BudgetFlow.Application.Wallets;

public sealed record CreateWalletRequest(
    string Name,
    string Currency,
    decimal InitialBalance);
