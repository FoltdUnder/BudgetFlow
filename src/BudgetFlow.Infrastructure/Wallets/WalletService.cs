using BudgetFlow.Application.Common.Exceptions;
using BudgetFlow.Application.Wallets;
using BudgetFlow.Domain.Entities;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetFlow.Infrastructure.Wallets;

public sealed class WalletService : IWalletService
{
    private readonly AppDbContext _dbContext;

    public WalletService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WalletDto> CreateAsync(
        Guid userId,
        CreateWalletRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Wallet name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Currency))
        {
            throw new ValidationException("Wallet currency is required.");
        }

        if (request.InitialBalance < 0)
        {
            throw new ValidationException("Initial balance cannot be negative.");
        }

        var userExists = await _dbContext.Users
            .AnyAsync(x => x.Id == userId, cancellationToken);

        if (!userExists)
        {
            throw new NotFoundException($"User '{userId}' was not found.");
        }

        var wallet = new Wallet(
            userId,
            request.Name.Trim(),
            request.Currency.Trim(),
            request.InitialBalance);

        _dbContext.Wallets.Add(wallet);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new WalletDto(
            wallet.Id,
            wallet.UserId,
            wallet.Name,
            wallet.Currency,
            wallet.Balance,
            wallet.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<WalletDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Wallets
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new WalletDto(
                x.Id,
                x.UserId,
                x.Name,
                x.Currency,
                x.Balance,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid userId,
        Guid walletId,
        CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(
                x => x.Id == walletId && x.UserId == userId,
                cancellationToken);

        if (wallet is null)
        {
            throw new NotFoundException($"Wallet '{walletId}' was not found.");
        }

        if (!wallet.IsEmpty)
        {
            throw new ValidationException("Only empty wallets can be deleted.");
        }

        _dbContext.Wallets.Remove(wallet);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
