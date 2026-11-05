using ITHelpdesk.Web.Models.Enums;

namespace ITHelpdesk.Web.DTOs.Notifications;

public class NotificationItemDto
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
