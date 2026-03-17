using BudgetFlow.Domain.Types;

namespace BudgetFlow.Application.Transactions;

public sealed record TransactionDto(
    Guid Id,
    Guid UserId,
    Guid WalletId,
    Guid CategoryId,
    CategoryType Type,
    decimal Amount,
    DateTime Date,
    string? Note,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
