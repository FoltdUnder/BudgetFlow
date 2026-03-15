namespace BudgetFlow.Application.Authentication.Models;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FullName);