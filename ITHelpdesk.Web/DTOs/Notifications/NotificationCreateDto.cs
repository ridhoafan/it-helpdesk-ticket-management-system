using ITHelpdesk.Web.Models.Enums;

namespace ITHelpdesk.Web.DTOs.Notifications;

public class NotificationCreateDto
{
    public string UserId { get; set; } = string.Empty;

    public int TicketId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
