using System.ComponentModel.DataAnnotations;
using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Application.DTOs;

public class ConvertComplaintDto
{
    [Required]
    public Guid ComplaintId { get; set; }

    public string ComplaintTitle { get; set; } = string.Empty;
    public string ComplaintDescription { get; set; } = string.Empty;
    public string? ComplaintCategory { get; set; }
    public bool ComplaintIsAnonymous { get; set; }

    [Required]
    public CasePriority Priority { get; set; } = CasePriority.Medium;
}

public record ConvertComplaintResult(
    bool Success,
    Guid? CaseId = null,
    IEnumerable<string>? Errors = null);