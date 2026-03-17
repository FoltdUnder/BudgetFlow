using BudgetFlow.Application.Common.Exceptions;
using BudgetFlow.Application.Transactions;
using BudgetFlow.Domain.Entities;
using BudgetFlow.Domain.Types;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetFlow.Infrastructure.Services;

public sealed class TransactionService : ITransactionService
{
    private readonly AppDbContext _dbContext;

    public TransactionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TransactionDto> CreateAsync(
        Guid userId,
        CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.WalletId == Guid.Empty)
        {
            throw new ValidationException("Wallet identifier is required.");
        }

        if (request.CategoryId == Guid.Empty)
        {
            throw new ValidationException("Category identifier is required.");
        }

        if (request.Amount <= 0)
        {
            throw new ValidationException("Amount must be greater than zero.");
        }

        if (!IsSupportedType(request.Type))
        {
            throw new ValidationException("Category type must be correct.");
        }

        var wallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(
                x => x.Id == request.WalletId && x.UserId == userId,
                cancellationToken);

        if (wallet is null)
        {
            throw new NotFoundException($"Wallet '{request.WalletId}' was not found.");
        }

        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(
                x => x.Id == request.CategoryId && x.UserId == userId,
                cancellationToken);

        if (category is null)
        {
            throw new NotFoundException($"Category '{request.CategoryId}' was not found.");
        }

        if (category.Type != request.Type)
        {
            throw new ValidationException("Category type must match transaction type.");
        }

        if (request.Type == CategoryType.Income)
        {
            wallet.Deposit(request.Amount);
        }
        else
        {
            wallet.Withdraw(request.Amount);
        }

        var transaction = new Transaction(
            userId,
            request.WalletId,
            request.CategoryId,
            request.Type,
            request.Amount,
            request.Date,
            request.Note);

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapTransaction(transaction);
    }

    public async Task DeleteAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Transactions
            .Include(x => x.Wallet)
            .FirstOrDefaultAsync(
                x => x.Id == transactionId && x.UserId == userId,
                cancellationToken);

        if (transaction is null)
        {
            throw new NotFoundException($"Transaction '{transactionId}' was not found.");
        }

        if (transaction.Type == CategoryType.Income)
        {
            transaction.Wallet.Withdraw(transaction.Amount);
        }
        else
        {
            transaction.Wallet.Deposit(transaction.Amount);
        }

        _dbContext.Transactions.Remove(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsSupportedType(CategoryType type)
    {
        return type is CategoryType.Expense or CategoryType.Income;
    }

    private static TransactionDto MapTransaction(Transaction transaction)
    {
        return new TransactionDto(
            transaction.Id,
            transaction.UserId,
            transaction.WalletId,
            transaction.CategoryId,
            transaction.Type,
            transaction.Amount,
            transaction.Date,
            transaction.Note,
            transaction.CreatedAtUtc,
            transaction.UpdatedAtUtc);
    }
}
