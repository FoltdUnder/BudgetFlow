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
    private readonly IAdminDashboardService _adminDashboardService;

    public DashboardController(
        IUserDashboardService dashboardService,
        IAdminDashboardService adminDashboardService)
    {
        _dashboardService = dashboardService;
        _adminDashboardService = adminDashboardService;
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

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdmin(CancellationToken cancellationToken)
    {
        var dashboard = await _adminDashboardService.GetAsync(cancellationToken);
        return Ok(dashboard);
    }
}
