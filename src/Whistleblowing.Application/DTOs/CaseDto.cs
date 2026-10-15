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
    public string? ReportFindings { get; set; }
    public string? ReportRecommendation { get; set; }
    public IReadOnlyList<EvidenceItemDto> Evidence { get; set; } = Array.Empty<EvidenceItemDto>();
    public IReadOnlyList<CaseStatusHistoryItemDto> StatusHistory { get; set; } = Array.Empty<CaseStatusHistoryItemDto>();
    public IReadOnlyList<AdditionalInformationItemDto> AdditionalInformation { get; set; } = Array.Empty<AdditionalInformationItemDto>();
}

public class CaseStatusHistoryItemDto
{
    public CaseStatus Status { get; set; }
    public string? Note { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
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
    public bool HasReport { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
