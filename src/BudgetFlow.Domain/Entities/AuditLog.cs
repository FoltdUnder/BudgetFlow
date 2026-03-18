namespace BudgetFlow.Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid? UserId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public Guid? EntityId { get; private set; }

    public string? Description { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public User? User { get; private set; }

    private AuditLog()
    {
    }

    public AuditLog(Guid? userId, string action, string entityType, Guid? entityId = null, string? description = null)
    {
        UserId = userId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
