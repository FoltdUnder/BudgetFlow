using BudgetFlow.Application.Common.Exceptions;
using BudgetFlow.Application.Common.Interfaces;
using BudgetFlow.Application.Wallets;
using BudgetFlow.Domain.Entities;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetFlow.Infrastructure.Services;

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
}
