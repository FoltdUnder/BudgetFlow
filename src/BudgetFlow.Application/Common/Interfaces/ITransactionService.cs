using BudgetFlow.Application.Transactions;


namespace BudgetFlow.Application.Common.Interfaces;

public interface ITransactionService
{
    Task<TransactionDto> CreateAsync(Guid userId, CreateTransactionRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken);
}
