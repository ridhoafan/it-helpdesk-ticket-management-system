using System.ComponentModel.DataAnnotations;
using ITHelpdesk.Web.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace ITHelpdesk.Web.Models.Entities;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public AccountApprovalStatus ApprovalStatus { get; set; } = AccountApprovalStatus.Pending;

    public DateTime? ApprovalReviewedAtUtc { get; set; }

    public string? ApprovalReviewedByUserId { get; set; }

    public ApplicationUser? ApprovalReviewedByUser { get; set; }

    [MaxLength(500)]
    public string? ApprovalNote { get; set; }

    public ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();

    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();

    public ICollection<TicketComment> TicketComments { get; set; } = new List<TicketComment>();

    public ICollection<TicketStatusHistory> StatusChanges { get; set; } = new List<TicketStatusHistory>();

    public ICollection<ITSupportCategory> ITSupportCategories { get; set; } = new List<ITSupportCategory>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
