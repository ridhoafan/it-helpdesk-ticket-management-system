using ITHelpdesk.Web.Models.Enums;

namespace ITHelpdesk.Web.DTOs.Tickets;

public class TicketListItemDto
{
    public int Id { get; set; }

    public string TicketNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public TicketPriority Priority { get; set; }

    public TicketStatus Status { get; set; }

    public string CreatedByName { get; set; } = string.Empty;

    public string? AssignedToName { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}