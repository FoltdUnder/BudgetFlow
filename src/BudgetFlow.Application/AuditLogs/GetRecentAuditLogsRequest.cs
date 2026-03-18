namespace BudgetFlow.Application.AuditLogs;

public sealed record GetRecentAuditLogsRequest(
    int Limit = 20);
