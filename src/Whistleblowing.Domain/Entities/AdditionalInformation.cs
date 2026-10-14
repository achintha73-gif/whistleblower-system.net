using Whistleblowing.Domain.Common;
using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Domain.Entities;

public class AdditionalInformation : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public InformationType InformationType { get; set; } = InformationType.AdditionalDetails;

    public Guid ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;

    public Guid? SubmittedById { get; set; }
    public ApplicationUser? SubmittedBy { get; set; }
}