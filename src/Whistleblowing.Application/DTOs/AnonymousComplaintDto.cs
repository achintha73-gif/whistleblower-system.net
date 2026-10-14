using System.ComponentModel.DataAnnotations;
using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Application.DTOs;

public class SubmitAnonymousComplaintDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be 5-200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(5000, MinimumLength = 20, ErrorMessage = "Description must be 20-5000 characters")]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Category { get; set; }
}

public class TrackComplaintDto
{
    [Required(ErrorMessage = "Reference code is required")]
    [StringLength(20, MinimumLength = 5)]
    public string ReferenceCode { get; set; } = string.Empty;
}

public class AnonymousComplaintResultDto
{
    public Guid Id { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public ComplaintStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AddAnonymousInfoDto
{
    [Required]
    [StringLength(20, MinimumLength = 5)]
    public string ReferenceCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    public InformationType InformationType { get; set; } = InformationType.AdditionalDetails;
}

