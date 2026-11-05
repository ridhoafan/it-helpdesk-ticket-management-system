
using System.ComponentModel.DataAnnotations;
using ITHelpdesk.Web.Models.Enums;

namespace ITHelpdesk.Web.DTOs.Tickets;

public class UpdateTicketStatusDto
{
    [EnumDataType(typeof(TicketStatus))]
    public TicketStatus Status { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }
}
