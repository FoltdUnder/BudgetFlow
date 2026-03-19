using System.Security.Claims;
using BudgetFlow.Application.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetFlow.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize(Policy = "UserOnly")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var categories = await _categoryService.GetByUserIdAsync(userId, cancellationToken);
        return Ok(categories);
    }
}
