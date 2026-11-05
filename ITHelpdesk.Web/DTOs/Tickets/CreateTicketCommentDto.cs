using System.ComponentModel.DataAnnotations;

namespace ITHelpdesk.Web.DTOs.Tickets;

public class CreateTicketCommentDto
{
    [Required(ErrorMessage = "Comment cannot be empty.")]
    [StringLength(
        2000,
        ErrorMessage = "Comment cannot exceed 2000 characters."
    )]
    public string Content { get; set; } = string.Empty;

    public bool IsInternal { get; set; } = false;
}