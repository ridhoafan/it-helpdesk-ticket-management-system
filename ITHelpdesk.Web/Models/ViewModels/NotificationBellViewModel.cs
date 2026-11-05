using ITHelpdesk.Web.DTOs.Notifications;

namespace ITHelpdesk.Web.Models.ViewModels;

public class NotificationBellViewModel
{
    public int UnreadCount { get; set; }

    public IReadOnlyList<NotificationItemDto> RecentNotifications { get; set; }
        = Array.Empty<NotificationItemDto>();
}
