using ITHelpdesk.Web.Models.Enums;

namespace ITHelpdesk.Web.DTOs.Tickets;

public class TicketDetailDto
{
    public int Id { get; set; }

    public string TicketNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public TicketPriority Priority { get; set; }

    public TicketStatus Status { get; set; }

    public string CreatedByName { get; set; } = string.Empty;

    public string? AssignedToName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<TicketCommentDto> Comments { get; set; } = new();

    public List<TicketStatusHistoryDto> StatusHistories { get; set; } = new();
}