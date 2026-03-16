using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BudgetFlow.Domain.Entities;

namespace BudgetFlow.Api.Controllers;

[ApiController]
[Route("api/test")]
[Authorize]
public sealed class TestController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            Message = "You are authenticated",
            UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
        });
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("admin")]
    public IActionResult Admin()
    {
        return Ok(new
        {
            Message = "Admin access granted"
        });
    }
}
