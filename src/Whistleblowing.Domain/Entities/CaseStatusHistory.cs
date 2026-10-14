using Whistleblowing.Domain.Common;
using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Domain.Entities;

public class CaseStatusHistory : BaseEntity
{
    public Guid CaseId { get; set; }
    public Case Case { get; set; } = null!;

    public CaseStatus Status { get; set; }
    public string? Note { get; set; }

    public Guid ChangedById { get; set; }
    public ApplicationUser ChangedBy { get; set; } = null!;

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}