using BudgetFlow.Application.AuditLogs;
using BudgetFlow.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetFlow.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent(
        [FromQuery] GetRecentAuditLogsRequest request,
        CancellationToken cancellationToken)
    {
        var auditLogs = await _auditLogService.GetRecentAsync(request, cancellationToken);
        return Ok(auditLogs);
    }
}
