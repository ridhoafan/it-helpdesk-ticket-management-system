
namespace ITHelpdesk.Web.Models.Entities;

public class ITSupportCategory
{
    // IT Support user reference
    public string ITSupportUserId { get; set; } = string.Empty;

    public ApplicationUser ITSupportUser { get; set; } = null!;

    // Ticket category reference
    public int TicketCategoryId { get; set; }

    public TicketCategory TicketCategory { get; set; } = null!;

    // When the specialization was assigned by Admin
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
