using System.ComponentModel.DataAnnotations;
using ITHelpdesk.Web.Models.Enums;

namespace ITHelpdesk.Web.DTOs.Tickets;

public class UpdateTicketDto
{
    [Required]
    [StringLength(150, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    public TicketPriority Priority { get; set; }
}