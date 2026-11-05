
using System.Security.Claims;
using ITHelpdesk.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHelpdesk.Web.Controllers;

[Authorize(Roles = "Employee")]
public class EmployeeController : Controller
{
    private readonly IDashboardService _dashboardService;

    public EmployeeController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    // ==========================================
    // EMPLOYEE PORTAL
    // GET: /Employee
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Get the identity of the currently logged-in employee.
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        // Retrieve only this employee's ticket statistics.
        var dashboard = await _dashboardService.GetSummaryAsync(
            userId
        );

        return View(dashboard);
    }
}
