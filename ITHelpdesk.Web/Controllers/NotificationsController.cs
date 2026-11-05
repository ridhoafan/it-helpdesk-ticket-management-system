using System.Security.Claims;
using ITHelpdesk.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHelpdesk.Web.Controllers;

[Authorize(Roles = "Admin,IT Support,Employee")]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly ITicketService _ticketService;

    public NotificationsController(
        INotificationService notificationService,
        ITicketService ticketService)
    {
        _notificationService = notificationService;
        _ticketService = ticketService;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var userId = CurrentUserId();
        if (userId == null) return Challenge();

        var model = await _notificationService.GetPageAsync(userId, page);
        ViewBag.UnreadCount = await _notificationService
            .GetUnreadCountAsync(userId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id, int page = 1)
    {
        var userId = CurrentUserId();
        if (userId == null) return Challenge();

        await _notificationService.MarkAsReadAsync(id, userId);
        return RedirectToAction(nameof(Index), new { page });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead(int page = 1)
    {
        var userId = CurrentUserId();
        if (userId == null) return Challenge();

        await _notificationService.MarkAllAsReadAsync(userId);
        return RedirectToAction(nameof(Index), new { page });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(int id)
    {
        var userId = CurrentUserId();
        if (userId == null) return Challenge();

        var notification = await _notificationService
            .GetForUserAsync(id, userId);
        if (notification == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        var isSupport = User.IsInRole("IT Support") && !isAdmin;
        var isEmployee = User.IsInRole("Employee") && !isAdmin
            && !User.IsInRole("IT Support");

        var ticket = await _ticketService.GetByIdAsync(
            notification.TicketId,
            createdByUserId: isEmployee ? userId : null,
            assignedToUserId: isSupport ? userId : null);

        if (ticket == null) return NotFound();

        await _notificationService.MarkAsReadAsync(id, userId);
        return RedirectToAction("Details", "Tickets",
            new { id = notification.TicketId });
    }

    private string? CurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);
}
