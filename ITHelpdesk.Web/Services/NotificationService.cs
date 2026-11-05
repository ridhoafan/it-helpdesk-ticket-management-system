using ITHelpdesk.Web.Data;
using ITHelpdesk.Web.DTOs.Notifications;
using ITHelpdesk.Web.Models.Entities;
using ITHelpdesk.Web.Models.Enums;
using ITHelpdesk.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ITHelpdesk.Web.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateManyAsync(
        IEnumerable<NotificationCreateDto> notifications)
    {
        var requested = notifications
            .Where(n => !string.IsNullOrWhiteSpace(n.UserId)
                && n.TicketId > 0
                && Enum.IsDefined(n.Type)
                && !string.IsNullOrWhiteSpace(n.Title)
                && !string.IsNullOrWhiteSpace(n.Message))
            .GroupBy(n => new { n.UserId, n.TicketId, n.Type })
            .Select(g => g.First())
            .ToList();

        if (requested.Count == 0)
        {
            return;
        }

        var userIds = requested
            .Select(n => n.UserId)
            .Distinct()
            .ToArray();

        var eligibleUserIds = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id)
                && u.IsActive
                && u.ApprovalStatus == AccountApprovalStatus.Approved)
            .Select(u => u.Id)
            .ToListAsync();

        var eligibleSet = eligibleUserIds.ToHashSet();
        var entities = requested
            .Where(n => eligibleSet.Contains(n.UserId))
            .Select(n => new Notification
            {
                UserId = n.UserId,
                TicketId = n.TicketId,
                Type = n.Type,
                Title = n.Title.Trim()[..Math.Min(n.Title.Trim().Length, 150)],
                Message = n.Message.Trim()[..Math.Min(n.Message.Trim().Length, 500)],
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        if (entities.Count == 0)
        {
            return;
        }

        _context.Notifications.AddRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<NotificationItemDto>> GetRecentAsync(
        string userId,
        int count = 5)
    {
        count = Math.Clamp(count, 1, 5);

        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ThenByDescending(n => n.Id)
            .Take(count)
            .Select(n => new NotificationItemDto
            {
                Id = n.Id,
                TicketId = n.TicketId,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAtUtc = n.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<NotificationPageDto> GetPageAsync(
        string userId,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(
            1,
            (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationItemDto
            {
                Id = n.Id,
                TicketId = n.TicketId,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAtUtc = n.CreatedAtUtc
            })
            .ToListAsync();

        return new NotificationPageDto
        {
            Items = items,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public Task<int> GetUnreadCountAsync(string userId)
    {
        return _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public Task<NotificationItemDto?> GetForUserAsync(
        int id,
        string userId)
    {
        return _context.Notifications
            .AsNoTracking()
            .Where(n => n.Id == id && n.UserId == userId)
            .Select(n => new NotificationItemDto
            {
                Id = n.Id,
                TicketId = n.TicketId,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAtUtc = n.CreatedAtUtc
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> MarkAsReadAsync(int id, string userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public Task<int> MarkAllAsReadAsync(string userId)
    {
        return _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true));
    }
}
