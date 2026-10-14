using Whistleblowing.Domain.Common;

namespace Whistleblowing.Domain.Entities;

public class CaseInvestigator : BaseEntity
{
    public Guid CaseId { get; set; }
    public Case Case { get; set; } = null!;

    public Guid InvestigatorId { get; set; }
    public ApplicationUser Investigator { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}