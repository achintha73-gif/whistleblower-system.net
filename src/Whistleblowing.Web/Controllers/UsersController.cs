using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Whistleblowing.Application.DTOs;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class UsersController : Controller
{
    private readonly IUserManagementService _userService;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(
        IUserManagementService userService,
        UserManager<ApplicationUser> userManager)
    {
        _userService = userService;
        _userManager = userManager;
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
}