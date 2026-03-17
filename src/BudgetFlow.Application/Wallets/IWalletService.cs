namespace BudgetFlow.Application.Wallets;

public interface IWalletService
{
    Task<WalletDto> CreateAsync(Guid userId, CreateWalletRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WalletDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid walletId, CancellationToken cancellationToken);
}
