using BudgetFlow.Application.Common.Exceptions;
using BudgetFlow.Application.Transactions;
using BudgetFlow.Domain.Entities;
using BudgetFlow.Domain.Types;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetFlow.Infrastructure.Transactions;

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
        ValidateRequest(request.WalletId, request.CategoryId, request.Type, request.Date, request.Amount);

        var wallet = await GetWalletAsync(userId, request.WalletId, cancellationToken);
        var category = await GetCategoryAsync(userId, request.CategoryId, cancellationToken);

        EnsureMatchingCategoryType(category.Type, request.Type);
        ApplyTransactionEffect(wallet, request.Type, request.Amount);

        var transaction = new Transaction(
            userId,
            request.WalletId,
            request.CategoryId,
            request.Type,
            request.Amount,
            request.Date,
            request.Note);

        _dbContext.Transactions.Add(transaction);
        _dbContext.AuditLogs.Add(new AuditLog(
            userId,
            "transaction_created",
            nameof(Transaction),
            transaction.Id,
            $"Transaction '{transaction.Id}' was created."));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapTransaction(transaction);
    }

    public async Task<TransactionDto> UpdateAsync(
        Guid userId,
        Guid transactionId,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request.WalletId, request.CategoryId, request.Type, request.Date, request.Amount);

        var transaction = await _dbContext.Transactions
            .Include(x => x.Wallet)
            .FirstOrDefaultAsync(
                x => x.Id == transactionId && x.UserId == userId,
                cancellationToken);

        if (transaction is null)
        {
            throw new NotFoundException($"Transaction '{transactionId}' was not found.");
        }

        var wallet = await GetWalletAsync(userId, request.WalletId, cancellationToken);
        var category = await GetCategoryAsync(userId, request.CategoryId, cancellationToken);

        EnsureMatchingCategoryType(category.Type, request.Type);

        ReverseTransactionEffect(transaction.Wallet, transaction.Type, transaction.Amount);
        ApplyTransactionEffect(wallet, request.Type, request.Amount);

        transaction.ChangeWallet(wallet.Id);
        transaction.ChangeType(request.Type, category.Type);
        transaction.ChangeCategory(category.Id, category.Type);
        transaction.SetAmount(request.Amount);
        transaction.SetDate(request.Date);
        transaction.SetNote(request.Note);

        _dbContext.AuditLogs.Add(new AuditLog(
            userId,
            "transaction_updated",
            nameof(Transaction),
            transaction.Id,
            $"Transaction '{transaction.Id}' was updated."));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapTransaction(transaction);
    }

     public async Task<IReadOnlyList<TransactionDto>> GetByUserIdAsync(
        Guid userId,
        GetTransactionsRequest request,
        CancellationToken cancellationToken)
    {
        ValidateFilters(request);

        var query = _dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (request.WalletId.HasValue)
        {
            query = query.Where(x => x.WalletId == request.WalletId.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(x => x.Type == request.Type.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(x => x.Date >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(x => x.Date <= request.To.Value);
        }

        return await query
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new TransactionDto(
                x.Id,
                x.UserId,
                x.WalletId,
                x.CategoryId,
                x.Type,
                x.Amount,
                x.Date,
                x.Note,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
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

        ReverseTransactionEffect(transaction.Wallet, transaction.Type, transaction.Amount);

        _dbContext.AuditLogs.Add(new AuditLog(
            userId,
            "transaction_deleted",
            nameof(Transaction),
            transaction.Id,
            $"Transaction '{transaction.Id}' was deleted."));
        _dbContext.Transactions.Remove(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsSupportedType(CategoryType type)
    {
        return type is CategoryType.Expense or CategoryType.Income;
    }

    private static void ValidateFilters(GetTransactionsRequest request)
    {
        if (request.From.HasValue && request.To.HasValue && request.From.Value > request.To.Value)
        {
            throw new ValidationException("'from' must be less than or equal to 'to'.");
        }

        if (request.Type.HasValue && !IsSupportedType(request.Type.Value))
        {
            throw new ValidationException("Category type must be correct.");
        }
    }

    private static void ValidateRequest(Guid walletId, Guid categoryId, CategoryType type, DateTime date, decimal amount)
    {
        if (walletId == Guid.Empty)
        {
            throw new ValidationException("Wallet identifier is required.");
        }

        if (categoryId == Guid.Empty)
        {
            throw new ValidationException("Category identifier is required.");
        }

        if (amount <= 0)
        {
            throw new ValidationException("Amount must be greater than zero.");
        }

        if (date == default)
        {
            throw new ValidationException("Transaction date is required.");
        }

        if (!IsSupportedType(type))
        {
            throw new ValidationException("Category type must be correct.");
        }
    }

    private async Task<Wallet> GetWalletAsync(Guid userId, Guid walletId, CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(
                x => x.Id == walletId && x.UserId == userId,
                cancellationToken);

        if (wallet is null)
        {
            throw new NotFoundException($"Wallet '{walletId}' was not found.");
        }

        return wallet;
    }

    private async Task<Category> GetCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(
                x => x.Id == categoryId && x.UserId == userId,
                cancellationToken);

        if (category is null)
        {
            throw new NotFoundException($"Category '{categoryId}' was not found.");
        }

        return category;
    }

    private static void EnsureMatchingCategoryType(CategoryType categoryType, CategoryType transactionType)
    {
        if (categoryType != transactionType)
        {
            throw new ValidationException("Category type must match transaction type.");
        }
    }

    private static void ApplyTransactionEffect(Wallet wallet, CategoryType type, decimal amount)
    {
        if (type == CategoryType.Income)
        {
            wallet.Deposit(amount);
        }
        else
        {
            wallet.Withdraw(amount);
        }
    }

    private static void ReverseTransactionEffect(Wallet wallet, CategoryType type, decimal amount)
    {
        if (type == CategoryType.Income)
        {
            wallet.Withdraw(amount);
        }
        else
        {
            wallet.Deposit(amount);
        }
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
