using Whistleblowing.Domain.Common;

namespace Whistleblowing.Domain.Entities;

public class InvestigationReport : BaseEntity
{
    public Guid CaseId { get; set; }
    public Case Case { get; set; } = null!;

    public string Findings { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}