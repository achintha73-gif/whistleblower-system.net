using Whistleblowing.Domain.Common;

namespace Whistleblowing.Domain.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid? ManagerId { get; set; }
    public ApplicationUser? Manager { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}