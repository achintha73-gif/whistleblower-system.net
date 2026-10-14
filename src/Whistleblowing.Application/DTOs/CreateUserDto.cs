using System.ComponentModel.DataAnnotations;

namespace Whistleblowing.Application.DTOs;

public class CreateUserDto
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string RoleName { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }
}