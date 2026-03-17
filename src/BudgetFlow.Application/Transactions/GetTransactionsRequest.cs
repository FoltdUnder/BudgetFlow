using BudgetFlow.Domain.Types;

namespace BudgetFlow.Application.Transactions;

public sealed record GetTransactionsRequest(
    Guid? WalletId = null,
    CategoryType? Type = null,
    Guid? CategoryId = null,
    DateTime? From = null,
    DateTime? To = null);
