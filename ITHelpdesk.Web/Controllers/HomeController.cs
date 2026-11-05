
using System.Diagnostics;
using System.Security.Claims;
using ITHelpdesk.Web.Models;
using ITHelpdesk.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHelpdesk.Web.Controllers;

[Authorize(Roles = "Admin,IT Support,Employee")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IDashboardService _dashboardService;

    public HomeController(
        ILogger<HomeController> logger,
        IDashboardService dashboardService)
    {
        _logger = logger;
        _dashboardService = dashboardService;
    }

    // ==========================================
    // GET: /Home
    // ROLE-BASED DASHBOARD REDIRECT
    // ==========================================

    [HttpGet]
    public IActionResult Index()
    {
        var isAdmin = User.IsInRole("Admin");
        var isSupport = User.IsInRole("IT Support");
        var isEmployee = User.IsInRole("Employee");

        // Employee goes to the Employee Portal.
        if (isEmployee && !isAdmin && !isSupport)
        {
            return RedirectToAction(
                "Index",
                "Employee"
            );
        }

        // Admin and IT Support use the operational dashboard.
        if (isAdmin || isSupport)
        {
            return RedirectToAction(
                nameof(Dashboard)
            );
        }

        return Forbid();
    }

    // ==========================================
    // ADMIN / IT SUPPORT DASHBOARD
    // GET: /Home/Dashboard
    // ==========================================

    [Authorize(Roles = "Admin,IT Support")]
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var isAdmin = User.IsInRole("Admin");
        var isSupport = User.IsInRole("IT Support");

        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        // Admin sees all ticket statistics.
        // IT Support sees only assigned ticket statistics.
        var dashboard = await _dashboardService.GetSummaryAsync(
            assignedToUserId: isAdmin ? null : userId
        );

        ViewBag.IsAdmin = isAdmin;
        ViewBag.IsSupport = isSupport && !isAdmin;

        return View("Index", dashboard);
    }

    // ==========================================
    // GET: /Home/Privacy
    // ==========================================

    public IActionResult Privacy()
    {
        return View();
    }

    // ==========================================
    // GET: /Home/Error
    // ==========================================

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id
                ?? HttpContext.TraceIdentifier
        });
    }
}
