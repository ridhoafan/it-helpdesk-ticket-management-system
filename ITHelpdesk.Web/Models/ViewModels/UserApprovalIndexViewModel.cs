
namespace ITHelpdesk.Web.Models.ViewModels;

public class UserApprovalIndexViewModel
{
    public List<PendingUserItemViewModel> PendingUsers { get; set; }
        = new();

    public int PendingCount => PendingUsers.Count;
}

public class PendingUserItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
