using BudgetFlow.Domain.Entities;

namespace BudgetFlow.Application.Authentication;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
