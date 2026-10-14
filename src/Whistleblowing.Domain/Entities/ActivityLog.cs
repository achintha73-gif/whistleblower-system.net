using Whistleblowing.Domain.Common;

namespace Whistleblowing.Domain.Entities;

public class ActivityLog : BaseEntity
{
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? Metadata { get; set; }

    public Guid? ActorId { get; set; }
    public ApplicationUser? Actor { get; set; }
}