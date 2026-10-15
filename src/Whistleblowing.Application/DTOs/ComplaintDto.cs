using System.ComponentModel.DataAnnotations;
using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Application.DTOs;

public class ComplaintDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public ComplaintStatus Status { get; set; }
    public bool IsAnonymous { get; set; }
    public string? ReferenceCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public Guid? CaseId { get; set; }
    public int AdditionalInfoCount { get; set; }
    public int EvidenceCount { get; set; }
    public IReadOnlyList<EvidenceItemDto> Evidence { get; set; } = Array.Empty<EvidenceItemDto>();
    public IReadOnlyList<AdditionalInformationItemDto> AdditionalInformation { get; set; } = Array.Empty<AdditionalInformationItemDto>();
}

public class EvidenceItemDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? UploadedBy { get; set; }
}

public class AdditionalInformationItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InformationType { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}

public class CreateComplaintDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be 5-200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(5000, MinimumLength = 20, ErrorMessage = "Description must be 20-5000 characters")]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Category { get; set; }

    public bool IsAnonymous { get; set; }
}

public static class ComplaintCategories
{
    public static readonly string[] All =
    {
        "Fraud", "Financial", "Corruption", "Harassment", "Discrimination",
        "Safety", "Ethics", "Abuse of Power", "Other",
    };
}
