using ITHelpdesk.Web.Models.Enums;

namespace ITHelpdesk.Web.Models.Entities;

public class TicketStatusHistory
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public TicketStatus? OldStatus { get; set; }

    public TicketStatus NewStatus { get; set; }

    public string ChangedByUserId { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

    public Ticket Ticket { get; set; } = null!;

    public ApplicationUser ChangedByUser { get; set; } = null!;
}