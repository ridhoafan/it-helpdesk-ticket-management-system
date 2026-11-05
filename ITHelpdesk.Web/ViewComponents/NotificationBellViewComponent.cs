using System.Security.Claims;
using ITHelpdesk.Web.Models.ViewModels;
using ITHelpdesk.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ITHelpdesk.Web.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly INotificationService _notificationService;

    public NotificationBellViewComponent(
        INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = HttpContext.User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Content(string.Empty);
        }

        var model = new NotificationBellViewModel
        {
            UnreadCount = await _notificationService
                .GetUnreadCountAsync(userId),
            RecentNotifications = await _notificationService
                .GetRecentAsync(userId)
        };

        return View(model);
    }
}
