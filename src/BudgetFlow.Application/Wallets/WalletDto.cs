namespace BudgetFlow.Application.Wallets;

public sealed record WalletDto(
    Guid Id,
    Guid UserId,
    string Name,
    string Currency,
    decimal Balance,
    DateTime CreatedAtUtc);
