using Microsoft.AspNetCore.Identity;
using Whistleblowing.Domain.Common;
using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>, IAuditable, ISoftDeletable
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public string Status { get; set; } = UserStatus.Active;

    // Audit (IAuditable)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    // Soft delete (ISoftDeletable)
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    // Navigation properties
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
    public ICollection<Case> AssignedCases { get; set; } = new List<Case>();
    public ICollection<CaseInvestigator> InvestigatorAssignments { get; set; } = new List<CaseInvestigator>();
    public ICollection<Evidence> UploadedEvidence { get; set; } = new List<Evidence>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AdditionalInformation> AdditionalInfo { get; set; } = new List<AdditionalInformation>();
    public ICollection<CaseStatusHistory> CaseStatusChanges { get; set; } = new List<CaseStatusHistory>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}

public static class UserStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Suspended = "Suspended";
}