using System.Security.Claims;
using ServeFlow.Domain.Entities;
using ServeFlow.Infrastructure.Data;

namespace ServeFlow.Infrastructure.Services;

public class AuditLogService
{
    private readonly ServeFlowDbContext _dbContext;

    public AuditLogService(ServeFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(
        ClaimsPrincipal? user,
        string action,
        string entityType,
        string entityId,
        string description)
    {
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue("sub");

        var actorEmail = user?.FindFirstValue(ClaimTypes.Email)
            ?? user?.FindFirstValue("email")
            ?? user?.Identity?.Name
            ?? "Unknown";

        var auditLog = new AuditLog
        {
            UserId = userId,
            ActorEmail = actorEmail,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.AuditLogs.Add(auditLog);
    }
}