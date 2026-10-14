namespace Whistleblowing.Domain.Common;

/// <summary>
/// Marker interface for entities that record creation/modification.
/// Handled automatically by DbContext.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? CreatedByUserId { get; set; }
    Guid? UpdatedByUserId { get; set; }
}