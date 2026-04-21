using System.Security.Claims;
using BudgetFlow.Api.Controllers;
using BudgetFlow.Application.Categories;
using BudgetFlow.Domain.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BudgetFlow.Tests;

public sealed class CategoriesControllerTests
{
    [Fact]
    public async Task GetMine_WithoutNameIdentifierClaim_ReturnsUnauthorized()
    {
        var categoryService = new FakeCategoryService();
        var controller = CreateController(categoryService, []);

        var result = await controller.GetMine(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Null(categoryService.LastUserId);
    }

    [Fact]
    public async Task GetMine_WithInvalidNameIdentifierClaim_ReturnsUnauthorized()
    {
        var categoryService = new FakeCategoryService();
        var controller = CreateController(categoryService, [new Claim(ClaimTypes.NameIdentifier, "not-a-guid")]);

        var result = await controller.GetMine(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Null(categoryService.LastUserId);
    }

    [Fact]
    public async Task GetMine_WithValidNameIdentifierClaim_ReturnsOkAndCallsService()
    {
        var userId = Guid.NewGuid();
        var categoryService = new FakeCategoryService
        {
            Categories =
            [
                new CategoryDto(Guid.NewGuid(), "Groceries", CategoryType.Expense, true, null, DateTime.UtcNow)
            ]
        };
        var controller = CreateController(categoryService, [new Claim(ClaimTypes.NameIdentifier, userId.ToString())]);

        var result = await controller.GetMine(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<CategoryDto>>(okResult.Value);

        Assert.Equal(userId, categoryService.LastUserId);
        Assert.Single(payload);
        Assert.Equal("Groceries", payload[0].Name);
    }

    private static CategoriesController CreateController(ICategoryService categoryService, Claim[] claims)
    {
        var controller = new CategoriesController(categoryService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            }
        };

        return controller;
    }

    private sealed class FakeCategoryService : ICategoryService
    {
        public IReadOnlyList<CategoryDto> Categories { get; set; } = [];

        public Guid? LastUserId { get; private set; }

        public Task<IReadOnlyList<CategoryDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            LastUserId = userId;
            return Task.FromResult(Categories);
        }
    }
}
