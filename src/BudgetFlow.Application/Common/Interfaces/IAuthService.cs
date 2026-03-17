using BudgetFlow.Application.Authentication.Models;


namespace BudgetFlow.Application.Common.Interfaces;

public interface IAuthService
{
    Task<(string AccessToken, string RefreshToken)> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    Task<(string AccessToken, string RefreshToken)> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
}