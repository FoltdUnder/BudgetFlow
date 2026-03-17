using BudgetFlow.Domain.Types;

namespace BudgetFlow.Application.Transactions;

public sealed record UpdateTransactionRequest(
    Guid WalletId,
    Guid CategoryId,
    CategoryType Type,
    DateTime Date,
    decimal Amount,
    string? Note = null);
