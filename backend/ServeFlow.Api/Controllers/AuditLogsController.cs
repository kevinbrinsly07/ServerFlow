
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServeFlow.Infrastructure.Data;

namespace ServeFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly ServeFlowDbContext _dbContext;

    public AuditLogsController(ServeFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // GET: api/auditlogs
    // Admin only: view audit history with pagination and filters.
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Keep pagination values within safe limits.
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(action))
        {
            var actionFilter = action.Trim().ToLower();

            query = query.Where(log =>
                log.Action.ToLower() == actionFilter);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            var entityFilter = entityType.Trim().ToLower();

            query = query.Where(log =>
                log.EntityType.ToLower() == entityFilter);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();

            query = query.Where(log =>
                log.ActorEmail.ToLower().Contains(searchTerm) ||
                log.Description.ToLower().Contains(searchTerm) ||
                log.EntityId.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(log => log.CreatedAtUtc)
            .ThenByDescending(log => log.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new
            {
                log.Id,
                log.UserId,
                log.ActorEmail,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.Description,
                log.CreatedAtUtc
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)pageSize);

        return Ok(new
        {
            items,
            page,
            pageSize,
            totalCount,
            totalPages
        });
    }
}
