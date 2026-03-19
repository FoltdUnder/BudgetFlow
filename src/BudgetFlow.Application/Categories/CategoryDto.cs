using BudgetFlow.Domain.Types;

namespace BudgetFlow.Application.Categories;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    CategoryType Type,
    bool IsDefault,
    Guid? UserId,
    DateTime CreatedAtUtc);
