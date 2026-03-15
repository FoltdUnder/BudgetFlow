using BudgetFlow.Domain.Entities;

namespace BudgetFlow.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}