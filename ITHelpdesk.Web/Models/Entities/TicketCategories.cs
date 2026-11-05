namespace ITHelpdesk.Web.Models.Entities;

public class TicketCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Ticket> Tickets { get; set; }
        = new List<Ticket>();

    public ICollection<ITSupportCategory> ITSupportUsers { get; set; }
        = new List<ITSupportCategory>();
}