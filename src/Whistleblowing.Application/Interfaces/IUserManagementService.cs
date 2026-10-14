using Whistleblowing.Application.DTOs;

namespace Whistleblowing.Application.Interfaces;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(
        Guid currentUserId,
        string currentRole,
        CancellationToken ct = default);

    Task<CreateUserResult> CreateUserAsync(
        CreateUserDto input,
        CancellationToken ct = default);

    Task<IReadOnlyList<RoleOptionDto>> GetRoleOptionsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<DepartmentOptionDto>> GetDepartmentOptionsAsync(CancellationToken ct = default);
}

public record RoleOptionDto(string Name);
public record DepartmentOptionDto(Guid Id, string Name);