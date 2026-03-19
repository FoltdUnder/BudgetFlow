namespace BudgetFlow.Application.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
