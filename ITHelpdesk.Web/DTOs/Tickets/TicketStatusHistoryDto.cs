using ITHelpdesk.Web.Models.Enums;

namespace ITHelpdesk.Web.DTOs.Tickets;

public class TicketStatusHistoryDto
{
    public TicketStatus? OldStatus { get; set; }

    public TicketStatus NewStatus { get; set; }

    public string ChangedByName { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTime ChangedAtUtc { get; set; }
}