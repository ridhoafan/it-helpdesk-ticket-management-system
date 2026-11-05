using ITHelpdesk.Web.DTOs.Notifications;

namespace ITHelpdesk.Web.Services.Interfaces;

public interface INotificationService
{
    Task CreateManyAsync(
        IEnumerable<NotificationCreateDto> notifications);

    Task<IReadOnlyList<NotificationItemDto>> GetRecentAsync(
        string userId,
        int count = 5);

    Task<NotificationPageDto> GetPageAsync(
        string userId,
        int page = 1,
        int pageSize = 20);

    Task<int> GetUnreadCountAsync(string userId);

    Task<NotificationItemDto?> GetForUserAsync(
        int id,
        string userId);

    Task<bool> MarkAsReadAsync(int id, string userId);

    Task<int> MarkAllAsReadAsync(string userId);
}
