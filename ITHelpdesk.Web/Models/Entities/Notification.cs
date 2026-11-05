using System.ComponentModel.DataAnnotations;
using ITHelpdesk.Web.Models.Enums;

namespace ITHelpdesk.Web.Models.Entities;

public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int TicketId { get; set; }

    public Ticket Ticket { get; set; } = null!;

    public NotificationType Type { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
