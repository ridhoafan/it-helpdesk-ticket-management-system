
using ITHelpdesk.Web.Data;
using ITHelpdesk.Web.Models.Entities;
using ITHelpdesk.Web.Models.ViewModels;
using ITHelpdesk.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ITHelpdesk.Web.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;

    public AuditLogService(AppDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // RECORD USER ACTIVITY
    // ==========================================

    public async Task LogAsync(
        string? userId,
        string action,
        string entityType,
        string? entityId,
        string description,
        string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException(
                "Audit action cannot be empty.",
                nameof(action)
            );
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException(
                "Entity type cannot be empty.",
                nameof(entityType)
            );
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Audit description cannot be empty.",
                nameof(description)
            );
        }

        var auditLog = new AuditLog
        {
            UserId = string.IsNullOrWhiteSpace(userId)
                ? null
                : userId,

            Action = action.Trim(),
            EntityType = entityType.Trim(),

            EntityId = string.IsNullOrWhiteSpace(entityId)
                ? null
                : entityId.Trim(),

            Description = description.Trim(),

            IpAddress = string.IsNullOrWhiteSpace(ipAddress)
                ? null
                : ipAddress.Trim(),

            CreatedAtUtc = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();
    }

    // ==========================================
    // RETRIEVE AUDIT LOGS
    // ==========================================

    public async Task<AuditLogIndexViewModel> GetLogsAsync(
        string? search = null,
        string? actionFilter = null,
        int page = 1,
        int pageSize = 20)
    {
        // Validate pagination values.
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Retrieve audit logs.
        var query = _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .AsQueryable();

        // ==========================================
        // SEARCH FILTER
        // ==========================================

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(a =>
                a.Action.Contains(search) ||
                a.EntityType.Contains(search) ||
                (a.EntityId != null &&
                    a.EntityId.Contains(search)) ||
                a.Description.Contains(search) ||
                (a.User != null &&
                    a.User.FullName.Contains(search)) ||
                (a.User != null &&
                    a.User.Email != null &&
                    a.User.Email.Contains(search)) ||
                (a.IpAddress != null &&
                    a.IpAddress.Contains(search))
            );
        }

        // ==========================================
        // ACTION FILTER
        // ==========================================

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            actionFilter = actionFilter.Trim();

            query = query.Where(a =>
                a.Action == actionFilter);
        }

        // ==========================================
        // TOTAL COUNT
        // ==========================================

        var totalCount = await query.CountAsync();

        // ==========================================
        // PAGINATION AND DATA PROJECTION
        // ==========================================

        var logs = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogItemViewModel
            {
                Id = a.Id,

                UserName = a.User != null
                    ? a.User.FullName
                    : "Deleted User",

                UserEmail = a.User != null
                    ? a.User.Email
                    : null,

                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Description = a.Description,
                IpAddress = a.IpAddress,
                CreatedAtUtc = a.CreatedAtUtc
            })
            .ToListAsync();

        // ==========================================
        // RETURN VIEW MODEL
        // ==========================================

        return new AuditLogIndexViewModel
        {
            Logs = logs,
            Search = search,
            ActionFilter = actionFilter,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }
}
