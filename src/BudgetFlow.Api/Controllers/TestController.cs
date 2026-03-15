using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetFlow.Api.Controllers;

[ApiController]
[Route("api/test")]
public sealed class TestController : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            Message = "You are authenticated",
            UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        });
    }
}