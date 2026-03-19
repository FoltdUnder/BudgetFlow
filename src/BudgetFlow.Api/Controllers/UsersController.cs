using BudgetFlow.Application.Users;
using BudgetFlow.Api.Contracts;
using BudgetFlow.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BudgetFlow.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    [Authorize(Policy = "UserOnly")]
    public IActionResult Me()
    {
        return Ok(new
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Email = User.FindFirstValue(ClaimTypes.Email),
            Role = User.FindFirstValue(ClaimTypes.Role)
        });
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(actorUserIdValue, out var actorUserId))
        {
            return Unauthorized();
        }

        await _userService.ChangeRoleAsync(actorUserId, id, request.Role, cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/block")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Block(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actorUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(actorUserIdValue, out var actorUserId))
        {
            return Unauthorized();
        }

        await _userService.BlockAsync(actorUserId, id, cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/unblock")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Unblock(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actorUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(actorUserIdValue, out var actorUserId))
        {
            return Unauthorized();
        }

        await _userService.UnblockAsync(actorUserId, id, cancellationToken);

        return NoContent();
    }

    [HttpGet("admin")]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult AdminOnly()
    {
        return Ok(new
        {
            Message = "You are authorized as Admin"
        });
    }
}
