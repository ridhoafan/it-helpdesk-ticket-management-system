namespace ITHelpdesk.Web.Models.ViewModels;

public class AuditLogIndexViewModel
{
    public List<AuditLogItemViewModel> Logs { get; set; }
        = new();

    public string? Search { get; set; }

    public string? ActionFilter { get; set; }

    public int TotalCount { get; set; }

    public int CurrentPage { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public int TotalPages =>
        (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class AuditLogItemViewModel
{
    public int Id { get; set; }

    public string? UserName { get; set; }

    public string? UserEmail { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}