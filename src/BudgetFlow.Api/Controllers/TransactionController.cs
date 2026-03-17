using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BudgetFlow.Application.Transactions;
using BudgetFlow.Application.Common.Interfaces;
using System.Security.Claims;

namespace BudgetFlow.Api.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize(Policy = "UserOnly")]
public sealed class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionController(
        ITransactionService transactionService
    )
    {
        _transactionService = transactionService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var transaction = await _transactionService.CreateAsync(userId, request, cancellationToken);
        return Created($"/api/transactions/{transaction.Id}", transaction);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        await _transactionService.DeleteAsync(userId, id, cancellationToken);
        return NoContent();
    }
}