using ITHelpdesk.Web.DTOs.Dashboard;

namespace ITHelpdesk.Web.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(
        string? createdByUserId = null,
        string? assignedToUserId = null
    );
}