namespace BudgetFlow.Application.AuditLogs;

public sealed record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? UserEmail,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? Description,
    DateTime CreatedAtUtc);
