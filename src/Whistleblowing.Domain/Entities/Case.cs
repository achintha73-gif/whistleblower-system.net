using Whistleblowing.Domain.Common;
using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Domain.Entities;

public class Case : BaseEntity
{
    public Guid ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;

    public CaseStatus Status { get; set; } = CaseStatus.Open;
    public CasePriority Priority { get; set; } = CasePriority.Medium;

    // Primary investigator (kept for backward compat)
    public Guid? AssignedInvestigatorId { get; set; }
    public ApplicationUser? AssignedInvestigator { get; set; }

    public DateTime? ClosedAt { get; set; }

    // Navigation
    public ICollection<CaseInvestigator> Investigators { get; set; } = new List<CaseInvestigator>();
    public ICollection<CaseStatusHistory> StatusHistory { get; set; } = new List<CaseStatusHistory>();
    public ICollection<Evidence> Evidence { get; set; } = new List<Evidence>();
    public InvestigationReport? InvestigationReport { get; set; }
}