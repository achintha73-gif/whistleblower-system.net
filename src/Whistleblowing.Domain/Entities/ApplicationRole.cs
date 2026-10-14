using Microsoft.AspNetCore.Identity;

namespace Whistleblowing.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public ApplicationRole() : base() { }

    public ApplicationRole(string roleName) : base(roleName) { }

    public ApplicationRole(string roleName, string description)
        : base(roleName)
    {
        Description = description;
    }
}