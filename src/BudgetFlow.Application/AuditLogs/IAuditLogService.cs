namespace BudgetFlow.Application.AuditLogs;

public interface IAuditLogService
{
    Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(
        GetRecentAuditLogsRequest request,
        CancellationToken cancellationToken);
}
