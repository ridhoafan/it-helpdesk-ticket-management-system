namespace ITHelpdesk.Web.Models.Enums;

public enum NotificationType
{
    TicketAssigned = 0,
    TicketAssignmentPending = 1,
    TicketReassigned = 2,
    TicketStatusChanged = 3,
    NewPublicComment = 4,
    NewInternalNote = 5
}
