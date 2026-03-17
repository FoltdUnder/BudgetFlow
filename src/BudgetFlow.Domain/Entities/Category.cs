using BudgetFlow.Domain.Types;

namespace BudgetFlow.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public CategoryType Type { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }
}