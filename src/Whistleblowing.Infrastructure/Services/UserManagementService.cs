using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Whistleblowing.Application.DTOs;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Domain.Enums;
using Whistleblowing.Infrastructure.Data;

namespace Whistleblowing.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UserManagementService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // -------------------------------------------------------------
    // List
    // -------------------------------------------------------------

    public async Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(
        Guid currentUserId,
        string currentRole,
        CancellationToken ct = default)
    {
        var query = _db.Users
            .Include(u => u.Department)
            .AsNoTracking()
            .Where(u => !u.IsDeleted)
            .AsQueryable();

        // Manager scope: only same department
        if (currentRole == RoleNames.Manager)
        {
            var currentUser = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == currentUserId, ct);

            if (currentUser?.DepartmentId == null)
                return Array.Empty<UserListItemDto>();

            query = query.Where(u => u.DepartmentId == currentUser.DepartmentId);
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(ct);

        var userIds = users.Select(u => u.Id).ToList();
        var userRoles = await _db.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(
                _db.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, RoleName = r.Name })
            .ToListAsync(ct);

        var roleLookup = userRoles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.First().RoleName ?? "Unknown");

        return users.Select(u => new UserListItemDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email ?? string.Empty,
            Phone = u.Phone,
            Status = u.Status,
            RoleName = roleLookup.TryGetValue(u.Id, out var role) ? role : "Unknown",
            DepartmentId = u.DepartmentId,
            DepartmentName = u.Department?.Name,
            CreatedAt = u.CreatedAt,
        }).ToList();
    }

    // -------------------------------------------------------------
    // Create
    // -------------------------------------------------------------

    public async Task<CreateUserResult> CreateUserAsync(
        CreateUserDto input,
        CancellationToken ct = default)
    {
        var errors = new List<string>();

        // Validate role
        if (!RoleNames.All.Contains(input.RoleName))
        {
            return new CreateUserResult(false, Errors: new[] { "Invalid role" });
        }

        // Check email uniqueness
        var existing = await _userManager.FindByEmailAsync(input.Email);
        if (existing != null)
        {
            return new CreateUserResult(false, Errors: new[] { "Email is already registered" });
        }

        // Validate department if provided
        if (input.DepartmentId.HasValue)
        {
            var deptExists = await _db.Departments
                .AnyAsync(d => d.Id == input.DepartmentId.Value, ct);
            if (!deptExists)
                return new CreateUserResult(false, Errors: new[] { "Department not found" });
        }

        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email,
            EmailConfirmed = true,
            FullName = input.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim(),
            Status = UserStatus.Active,
            DepartmentId = input.DepartmentId,
        };

        var result = await _userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            return new CreateUserResult(
                false,
                Errors: result.Errors.Select(e => e.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, input.RoleName);
        if (!roleResult.Succeeded)
        {
            // Rollback
            await _userManager.DeleteAsync(user);
            return new CreateUserResult(
                false,
                Errors: roleResult.Errors.Select(e => e.Description));
        }

        return new CreateUserResult(true, user.Id);
    }

    // -------------------------------------------------------------
    // Options (for dropdowns)
    // -------------------------------------------------------------

    public async Task<IReadOnlyList<RoleOptionDto>> GetRoleOptionsAsync(
        CancellationToken ct = default)
    {
        return await Task.FromResult(
            RoleNames.All.Select(r => new RoleOptionDto(r)).ToList());
    }

    public async Task<IReadOnlyList<DepartmentOptionDto>> GetDepartmentOptionsAsync(
        CancellationToken ct = default)
    {
        return await _db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentOptionDto(d.Id, d.Name))
            .ToListAsync(ct);
    }
}
