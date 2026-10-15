using System.ComponentModel.DataAnnotations;

namespace Whistleblowing.Web.Models.Users;

public class EditUserViewModel
{
    public Guid Id { get; set; }

    [Required, StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? Phone { get; set; }

    [Required]
    public string RoleName { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }

    [Required]
    public string Status { get; set; } = "Active";
}
