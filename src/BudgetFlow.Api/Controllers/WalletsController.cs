using System.Security.Claims;
using BudgetFlow.Application.Common.Interfaces;
using BudgetFlow.Application.Wallets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetFlow.Api.Controllers;

[ApiController]
[Route("api/wallets")]
[Authorize(Policy = "UserOnly")]
public sealed class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletsController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateWalletRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var wallet = await _walletService.CreateAsync(userId, request, cancellationToken);
        return Created($"/api/wallets/{wallet.Id}", wallet);
    }
}
