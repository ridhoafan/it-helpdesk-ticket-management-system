
using ITHelpdesk.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ITHelpdesk.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AuditLogsController : Controller
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    // ==========================================
    // GET: /AuditLogs
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? actionFilter,
        int page = 1)
    {
        // Retrieve audit logs with search and pagination.
        var model = await _auditLogService.GetLogsAsync(
            search,
            actionFilter,
            page,
            pageSize: 20
        );

        // Available activity filters.
        var actionOptions = new[]
        {
            "TicketCreated",
            "TicketUpdated",
            "TicketStatusChanged",
            "TicketDeleted",
            "UserApproved",
            "UserRejected"
        };

        ViewBag.ActionOptions = new SelectList(
            actionOptions,
            actionFilter
        );

        return View(model);
    }
}
