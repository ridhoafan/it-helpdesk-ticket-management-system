
using ITHelpdesk.Web.Models.ViewModels;

namespace ITHelpdesk.Web.Services.Interfaces;

public interface IAuditLogService
{
    // Record an activity performed by a user.
    Task LogAsync(
        string? userId,
        string action,
        string entityType,
        string? entityId,
        string description,
        string? ipAddress = null
    );

    // Retrieve audit logs with optional filters and pagination.
    Task<AuditLogIndexViewModel> GetLogsAsync(
        string? search = null,
        string? actionFilter = null,
        int page = 1,
        int pageSize = 20
    );
}
