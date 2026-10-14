using Whistleblowing.Domain.Common;
using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Domain.Entities;

public class Complaint : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }

    public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;

    public bool IsAnonymous { get; set; }
    public string? ReferenceCode { get; set; }

    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Navigation
    public Case? Case { get; set; }
    public ICollection<Evidence> Evidence { get; set; } = new List<Evidence>();
    public ICollection<AdditionalInformation> AdditionalInfo { get; set; } = new List<AdditionalInformation>();
}