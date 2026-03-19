using BudgetFlow.Application.Categories;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetFlow.Infrastructure.Categories;

public sealed class CategoryService : ICategoryService
{
    private readonly AppDbContext _dbContext;

    public CategoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.IsDefault || x.UserId == userId)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Type)
            .ThenBy(x => x.Name)
            .Select(x => new CategoryDto(
                x.Id,
                x.Name,
                x.Type,
                x.IsDefault,
                x.UserId,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
