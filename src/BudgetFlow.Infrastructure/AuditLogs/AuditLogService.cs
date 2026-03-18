using BudgetFlow.Application.AuditLogs;
using BudgetFlow.Application.Common.Exceptions;
using BudgetFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetFlow.Infrastructure.AuditLogs;

public sealed class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _dbContext;

    public AuditLogService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(
        GetRecentAuditLogsRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        return await _dbContext.AuditLogs
            .AsNoTracking()
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(request.Limit)
            .Select(x => new AuditLogDto(
                x.Id,
                x.UserId,
                x.User != null ? x.User.Email : null,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.Description,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private static void ValidateRequest(GetRecentAuditLogsRequest request)
    {
        if (request.Limit <= 0)
        {
            throw new ValidationException("Limit must be greater than zero.");
        }

        if (request.Limit > 100)
        {
            throw new ValidationException("Limit cannot be greater than 100.");
        }
    }
}
