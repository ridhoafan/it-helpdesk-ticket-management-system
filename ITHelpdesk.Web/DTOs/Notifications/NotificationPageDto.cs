namespace ITHelpdesk.Web.DTOs.Notifications;

public class NotificationPageDto
{
    public IReadOnlyList<NotificationItemDto> Items { get; set; }
        = Array.Empty<NotificationItemDto>();

    public int CurrentPage { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages =>
        (int)Math.Ceiling(TotalCount / (double)PageSize);
}
