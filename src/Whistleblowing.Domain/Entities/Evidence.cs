using Whistleblowing.Domain.Common;

namespace Whistleblowing.Domain.Entities;

public class Evidence : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
    public string? Description { get; set; }

    public Guid? CaseId { get; set; }
    public Case? Case { get; set; }

    public Guid? ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }

    public Guid? UploadedById { get; set; }
    public ApplicationUser? UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}