
using ITHelpdesk.Web.DTOs.Tickets;

namespace ITHelpdesk.Web.DTOs.Dashboard;

public class DashboardSummaryDto
{
    // Total active tickets
    public int TotalTickets { get; set; }

    // Ticket count by status
    public int OpenTickets { get; set; }

    public int InProgressTickets { get; set; }

    public int ResolvedTickets { get; set; }

    public int ClosedTickets { get; set; }

    // Ticket count by priority
    public int LowPriorityTickets { get; set; }

    public int MediumPriorityTickets { get; set; }

    public int HighPriorityTickets { get; set; }

    public int CriticalPriorityTickets { get; set; }

    // Latest tickets displayed on dashboard
    public List<TicketListItemDto> RecentTickets { get; set; }
        = new List<TicketListItemDto>();
}
