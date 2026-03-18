using System.Security.Claims;
using BudgetFlow.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetFlow.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "UserOnly")]
public sealed class DashboardController : ControllerBase
{
    private readonly IUserDashboardService _dashboardService;

    public DashboardController(IUserDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var dashboard = await _dashboardService.GetAsync(userId, cancellationToken);
        return Ok(dashboard);
    }
}
