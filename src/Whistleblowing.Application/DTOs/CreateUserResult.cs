namespace Whistleblowing.Application.DTOs;

public record CreateUserResult(bool Success, Guid? UserId = null, IEnumerable<string>? Errors = null);