using BudgetFlow.Application.Wallets;

namespace BudgetFlow.Application.Common.Interfaces;

public interface IWalletService
{
    Task<WalletDto> CreateAsync(Guid userId, CreateWalletRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WalletDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
