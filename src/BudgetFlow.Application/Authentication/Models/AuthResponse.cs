namespace BudgetFlow.Application.Authentication.Models;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken);