using System.ComponentModel.DataAnnotations;

namespace ITHelpdesk.Web.Models.Entities;

public class AuditLog
{
    public int Id { get; set; }

    // User who performed the action.
    // Nullable to preserve logs if a user account is removed.
    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    // Example: TicketDeleted, UserApproved.
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    // Example: Ticket, User.
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    // ID of the affected record.
    [MaxLength(100)]
    public string? EntityId { get; set; }

    // Human-readable activity description.
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    // User IP address when the action occurred.
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}