using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Application.DTOs;

public class CaseDto
{
    public Guid Id { get; set; }

    public Guid ComplaintId { get; set; }
    public string ComplaintTitle { get; set; } = string.Empty;
    public string ComplaintDescription { get; set; } = string.Empty;
    public string? ComplaintCategory { get; set; }
    public bool ComplaintIsAnonymous { get; set; }

    public CaseStatus Status { get; set; }
    public CasePriority Priority { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Guid? AssignedInvestigatorId { get; set; }
    public string? AssignedInvestigatorName { get; set; }
    public string? AssignedInvestigatorEmail { get; set; }

    public int EvidenceCount { get; set; }
    public int StatusHistoryCount { get; set; }
    public int InvestigatorCount { get; set; }
    public bool HasReport { get; set; }
}

public class CaseListItemDto
{
    public Guid Id { get; set; }
    public string ComplaintTitle { get; set; } = string.Empty;
    public string? ComplaintCategory { get; set; }
    public bool ComplaintIsAnonymous { get; set; }
    public CaseStatus Status { get; set; }
    public CasePriority Priority { get; set; }
    public string? AssignedInvestigatorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}