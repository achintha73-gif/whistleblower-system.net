namespace Whistleblowing.Web.Models.Complaints;

public sealed class EmployeeInvestigationReportViewModel
{
    public Guid ComplaintId { get; init; }
    public Guid CaseId { get; init; }
    public string ConcernTitle { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string Findings { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; }
    public bool IsManagerReviewed { get; init; }
}
