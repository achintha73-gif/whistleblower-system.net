using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Whistleblowing.Application.DTOs;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Domain.Enums;
using Whistleblowing.Infrastructure.Data;
using Whistleblowing.Web.Models.Users;

namespace Whistleblowing.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class UsersController : Controller
{
    private readonly IUserManagementService _userService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public UsersController(
        IUserManagementService userService,
        UserManager<ApplicationUser> userManager,
        AppDbContext db)
    {
        _userService = userService;
        _userManager = userManager;
        _db = db;
    }

    // -------------------------------------------------------------
    // List
    // -------------------------------------------------------------

    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return RedirectToAction("Login", "Account");

        var roles = await _userManager.GetRolesAsync(currentUser);
        var primaryRole = roles.FirstOrDefault() ?? RoleNames.Employee;

        var users = await _userService.GetUsersAsync(currentUser.Id, primaryRole);

        ViewBag.CurrentUserId = currentUser.Id;
        ViewBag.CurrentRole = primaryRole;
        ViewBag.IsAdmin = primaryRole == RoleNames.Admin;
        ViewBag.CurrentUserId = currentUser.Id;

        return View(users);
    }

    // -------------------------------------------------------------
    // Create — Admin only
    // -------------------------------------------------------------

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await LoadOptionsAsync();
        return View(new CreateUserDto());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserDto model)
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return View(model);
        }

        var result = await _userService.CreateUserAsync(model);
        if (!result.Success)
        {
            foreach (var error in result.Errors ?? Enumerable.Empty<string>())
                ModelState.AddModelError(string.Empty, error);

            await LoadOptionsAsync();
            return View(model);
        }

        TempData["Success"] = "User created successfully";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        await LoadEditOptionsAsync();
        return View(new EditUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone,
            RoleName = roles.FirstOrDefault() ?? RoleNames.Employee,
            DepartmentId = user.DepartmentId,
            Status = user.Status
        });
    }

    [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id.ToString());
        if (user == null) return NotFound();

        if (!RoleNames.All.Contains(model.RoleName))
            ModelState.AddModelError(nameof(model.RoleName), "Choose a valid role.");
        if (model.Status != UserStatus.Active && model.Status != UserStatus.Inactive && model.Status != UserStatus.Suspended)
            ModelState.AddModelError(nameof(model.Status), "Choose a valid account status.");
        if (model.DepartmentId.HasValue && !await _db.Departments.AnyAsync(d => d.Id == model.DepartmentId.Value))
            ModelState.AddModelError(nameof(model.DepartmentId), "Choose a valid department.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentRole = currentRoles.FirstOrDefault() ?? RoleNames.Employee;
        var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);
        var changingOwnAccess = user.Id == currentUserId
            && (model.RoleName != currentRole || model.Status != UserStatus.Active);
        if (changingOwnAccess)
            ModelState.AddModelError(string.Empty, "You cannot change your own role or deactivate your account.");

        var removingLastActiveAdmin = currentRole == RoleNames.Admin
            && (model.RoleName != RoleNames.Admin || model.Status != UserStatus.Active);
        if (removingLastActiveAdmin)
        {
            var activeAdmins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
            if (!activeAdmins.Any(a => a.Id != user.Id && a.Status == UserStatus.Active))
                ModelState.AddModelError(string.Empty, "At least one active administrator must remain.");
        }

        if (ModelState.IsValid)
        {
            var duplicateEmail = await _userManager.FindByEmailAsync(model.Email);
            if (duplicateEmail != null && duplicateEmail.Id != user.Id)
                ModelState.AddModelError(nameof(model.Email), "That email address is already in use.");
        }

        if (!ModelState.IsValid)
        {
            await LoadEditOptionsAsync();
            return View(model);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();
        user.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        user.DepartmentId = model.DepartmentId;
        user.Status = model.Status;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedByUserId = currentUserId;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            await transaction.RollbackAsync();
            foreach (var error in updateResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
            await LoadEditOptionsAsync();
            return View(model);
        }

        if (currentRole != model.RoleName)
        {
            if (currentRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    foreach (var error in removeResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
                    await LoadEditOptionsAsync();
                    return View(model);
                }
            }
            var roleResult = await _userManager.AddToRoleAsync(user, model.RoleName);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                foreach (var error in roleResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
                await LoadEditOptionsAsync();
                return View(model);
            }
        }

        _db.ActivityLogs.Add(new ActivityLog
        {
            EventType = "UserUpdated",
            Description = $"Administrator updated account for {user.FullName}.",
            EntityType = "ApplicationUser",
            EntityId = user.Id,
            ActorId = currentUserId
        });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = "User updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id)
    {
        var actor = await _userManager.GetUserAsync(User);
        var target = await _userManager.FindByIdAsync(id.ToString());
        if (actor == null) return RedirectToAction("Login", "Account");
        if (target == null || target.IsDeleted) return NotFound();
        if (target.Id == actor.Id)
        {
            TempData["Error"] = "You cannot archive your own account.";
            return RedirectToAction(nameof(Index));
        }

        var targetRoles = await _userManager.GetRolesAsync(target);
        if (targetRoles.Contains(RoleNames.Admin) && target.Status == UserStatus.Active)
        {
            var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
            if (!admins.Any(admin => admin.Id != target.Id && !admin.IsDeleted && admin.Status == UserStatus.Active))
            {
                TempData["Error"] = "At least one active administrator must remain.";
                return RedirectToAction(nameof(Index));
            }
        }

        target.IsDeleted = true;
        target.DeletedAt = DateTime.UtcNow;
        target.DeletedByUserId = actor.Id;
        target.UpdatedAt = DateTime.UtcNow;
        target.UpdatedByUserId = actor.Id;
        target.Status = UserStatus.Inactive;

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var result = await _userManager.UpdateAsync(target);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = string.Join(" ", result.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(Index));
        }
        await _userManager.UpdateSecurityStampAsync(target);
        _db.ActivityLogs.Add(new ActivityLog
        {
            EventType = "UserArchived",
            Description = $"Administrator archived account for {target.FullName}.",
            EntityType = "ApplicationUser",
            EntityId = target.Id,
            ActorId = actor.Id
        });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = "User account archived. Their complaint and case history remains available.";
        return RedirectToAction(nameof(Index));
    }

    // -------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------

    private async Task LoadOptionsAsync()
    {
        var roles = await _userService.GetRoleOptionsAsync();
        var departments = await _userService.GetDepartmentOptionsAsync();

        ViewBag.Roles = new SelectList(roles, "Name", "Name");
        ViewBag.Departments = new SelectList(departments, "Id", "Name");
    }

    private async Task LoadEditOptionsAsync()
    {
        var roles = await _userService.GetRoleOptionsAsync();
        var departments = await _userService.GetDepartmentOptionsAsync();
        ViewBag.Roles = new SelectList(roles, "Name", "Name");
        ViewBag.Departments = new SelectList(departments, "Id", "Name");
        ViewBag.Statuses = new SelectList(new[] { UserStatus.Active, UserStatus.Inactive, UserStatus.Suspended });
    }
}
