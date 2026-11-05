
using ITHelpdesk.Web.Data;
using ITHelpdesk.Web.DTOs.Dashboard;
using ITHelpdesk.Web.DTOs.Tickets;
using ITHelpdesk.Web.Models.Enums;
using ITHelpdesk.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ITHelpdesk.Web.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(
        string? createdByUserId = null,
        string? assignedToUserId = null)
    {
        // Retrieve active tickets only.
        var query = _context.Tickets
            .AsNoTracking()
            .Where(t => !t.IsDeleted);

        // Employee sees tickets they personally created.
        if (!string.IsNullOrWhiteSpace(createdByUserId))
        {
            query = query.Where(t =>
                t.CreatedByUserId == createdByUserId);
        }

        // IT Support sees tickets assigned to them.
        if (!string.IsNullOrWhiteSpace(assignedToUserId))
        {
            query = query.Where(t =>
                t.AssignedToUserId == assignedToUserId);
        }

        // ==========================================
        // TOTAL TICKETS
        // ==========================================

        var totalTickets = await query.CountAsync();

        // ==========================================
        // TICKETS BY STATUS
        // ==========================================

        var openTickets = await query.CountAsync(
            t => t.Status == TicketStatus.Open
        );

        var inProgressTickets = await query.CountAsync(
            t => t.Status == TicketStatus.InProgress
        );

        var resolvedTickets = await query.CountAsync(
            t => t.Status == TicketStatus.Resolved
        );

        var closedTickets = await query.CountAsync(
            t => t.Status == TicketStatus.Closed
        );

        // ==========================================
        // TICKETS BY PRIORITY
        // ==========================================

        var lowPriorityTickets = await query.CountAsync(
            t => t.Priority == TicketPriority.Low
        );

        var mediumPriorityTickets = await query.CountAsync(
            t => t.Priority == TicketPriority.Medium
        );

        var highPriorityTickets = await query.CountAsync(
            t => t.Priority == TicketPriority.High
        );

        var criticalPriorityTickets = await query.CountAsync(
            t => t.Priority == TicketPriority.Critical
        );

        // ==========================================
        // RECENT TICKETS
        // ==========================================

        var recentTickets = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(5)
            .Select(t => new TicketListItemDto
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                Title = t.Title,
                CategoryName = t.Category.Name,
                Priority = t.Priority,
                Status = t.Status,
                CreatedByName = t.CreatedByUser.FullName,
                AssignedToName = t.AssignedToUser != null
                    ? t.AssignedToUser.FullName
                    : null,
                CreatedAtUtc = t.CreatedAtUtc
            })
            .ToListAsync();

        // ==========================================
        // DASHBOARD SUMMARY
        // ==========================================

        var summary = new DashboardSummaryDto
        {
            TotalTickets = totalTickets,

            OpenTickets = openTickets,
            InProgressTickets = inProgressTickets,
            ResolvedTickets = resolvedTickets,
            ClosedTickets = closedTickets,

            LowPriorityTickets = lowPriorityTickets,
            MediumPriorityTickets = mediumPriorityTickets,
            HighPriorityTickets = highPriorityTickets,
            CriticalPriorityTickets = criticalPriorityTickets,

            RecentTickets = recentTickets
        };

        return summary;
    }
}
