using Whistleblowing.Domain.Common;

namespace Whistleblowing.Domain.Entities;

public class Notification : BaseEntity
{
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
}