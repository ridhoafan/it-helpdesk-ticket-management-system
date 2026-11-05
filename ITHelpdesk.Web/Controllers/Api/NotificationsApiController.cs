using System.Security.Claims;
using ITHelpdesk.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHelpdesk.Web.Controllers.Api;

[ApiController]
[Authorize(Roles = "Admin,IT Support,Employee")]
[Route("api/notifications")]
public class NotificationsApiController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsApiController(
        INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        return Ok(await _notificationService.GetPageAsync(
            userId, page, pageSize));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        return Ok(new
        {
            unreadCount = await _notificationService
                .GetUnreadCountAsync(userId)
        });
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        return await _notificationService.MarkAsReadAsync(id, userId)
            ? NoContent()
            : NotFound();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var updatedCount = await _notificationService
            .MarkAllAsReadAsync(userId);
        return Ok(new { updatedCount });
    }

    private string? CurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);
}
