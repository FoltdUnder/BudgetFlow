using BudgetFlow.Domain.Entities;
using BudgetFlow.Domain.Types;

namespace BudgetFlow.Infrastructure.Persistence;

internal static class DefaultCategoryFactory
{
    private static readonly (string Name, CategoryType Type)[] Definitions =
    [
        ("Salary", CategoryType.Income),
        ("Freelance", CategoryType.Income),
        ("Bonus", CategoryType.Income),
        ("Groceries", CategoryType.Expense),
        ("Rent", CategoryType.Expense),
        ("Transport", CategoryType.Expense),
        ("Food", CategoryType.Expense),
        ("Travel", CategoryType.Expense),
        ("Entertainment", CategoryType.Expense),
        ("Utilities", CategoryType.Expense)
    ];

    public static IReadOnlyList<Category> CreateDefaults(DateTime createdAtUtc)
    {
        return Definitions
            .Select(x => new Category
            {
                Id = Guid.NewGuid(),
                Name = x.Name,
                Type = x.Type,
                IsDefault = true,
                CreatedAtUtc = createdAtUtc
            })
            .ToList();
    }

    public static Category CreateCustom(Guid userId, string name, CategoryType type, DateTime createdAtUtc)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Type = type,
            IsDefault = false,
            CreatedAtUtc = createdAtUtc
        };
    }
}
