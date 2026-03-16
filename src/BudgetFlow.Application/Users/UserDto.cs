namespace BudgetFlow.Application.Users;

public sealed record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    bool IsBlocked,
    DateTime CreatedAtUtc);
