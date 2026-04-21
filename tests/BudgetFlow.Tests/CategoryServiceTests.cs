using BudgetFlow.Application.Categories;
using BudgetFlow.Domain.Entities;
using BudgetFlow.Domain.Types;
using BudgetFlow.Infrastructure.Categories;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetFlow.Tests;

public sealed class CategoryServiceTests
{
    [Fact]
    public async Task GetByUserIdAsync_ReturnsDefaultAndUserCategoriesOnly()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();

        dbContext.Categories.AddRange(
            CreateCategory("Default Expense", CategoryType.Expense, true, null),
            CreateCategory("User Expense", CategoryType.Expense, false, userId),
            CreateCategory("Other User Category", CategoryType.Income, false, Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var service = new CategoryService(dbContext);

        var result = await service.GetByUserIdAsync(userId, CancellationToken.None);

        Assert.Collection(
            result,
            category => Assert.Equal("Default Expense", category.Name),
            category => Assert.Equal("User Expense", category.Name));
    }

    [Fact]
    public async Task GetByUserIdAsync_OrdersByDefaultTypeAndName()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();

        dbContext.Categories.AddRange(
            CreateCategory("Salary", CategoryType.Income, true, null),
            CreateCategory("Bills", CategoryType.Expense, true, null),
            CreateCategory("Freelance", CategoryType.Income, false, userId),
            CreateCategory("Coffee", CategoryType.Expense, false, userId));
        await dbContext.SaveChangesAsync();

        var service = new CategoryService(dbContext);

        var result = await service.GetByUserIdAsync(userId, CancellationToken.None);

        Assert.Equal(
            ["Bills", "Salary", "Coffee", "Freelance"],
            result.Select(x => x.Name).ToArray());
    }

    [Fact]
    public async Task GetByUserIdAsync_MapsCategoryFieldsIntoDto()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 4, 21, 10, 30, 0, DateTimeKind.Utc);
        var categoryId = Guid.NewGuid();

        dbContext.Categories.Add(new Category
        {
            Id = categoryId,
            Name = "Investments",
            Type = CategoryType.Income,
            IsDefault = false,
            UserId = userId,
            CreatedAtUtc = createdAtUtc
        });
        await dbContext.SaveChangesAsync();

        var service = new CategoryService(dbContext);

        var result = await service.GetByUserIdAsync(userId, CancellationToken.None);
        var category = Assert.Single(result);

        Assert.Equal(new CategoryDto(categoryId, "Investments", CategoryType.Income, false, userId, createdAtUtc), category);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Category CreateCategory(string name, CategoryType type, bool isDefault, Guid? userId)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            IsDefault = isDefault,
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
