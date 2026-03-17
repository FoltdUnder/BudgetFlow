namespace BudgetFlow.Application.Transactions;

public interface ITransactionService
{
    Task<TransactionDto> CreateAsync(Guid userId, CreateTransactionRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
