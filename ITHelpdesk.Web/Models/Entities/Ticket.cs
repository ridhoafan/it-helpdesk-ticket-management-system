using ITHelpdesk.Web.Models.Enums;

namespace ITHelpdesk.Web.Models.Entities;

public class Ticket
{
    public int Id { get; set; }

    public string TicketNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public TicketPriority Priority { get; set; }
        = TicketPriority.Medium;

    public TicketStatus Status { get; set; }
        = TicketStatus.Open;

    public int CategoryId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public string? AssignedToUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Navigation properties

    public TicketCategory Category { get; set; } = null!;

    public ApplicationUser CreatedByUser { get; set; } = null!;

    public ApplicationUser? AssignedToUser { get; set; }

    public ICollection<TicketComment> Comments { get; set; }
        = new List<TicketComment>();

    public ICollection<TicketStatusHistory> StatusHistories { get; set; }
        = new List<TicketStatusHistory>();

    public ICollection<Notification> Notifications { get; set; }
        = new List<Notification>();
}
