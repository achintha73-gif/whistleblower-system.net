using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAdminStatsService _adminStats;

    public DashboardController(
        UserManager<ApplicationUser> userManager,
        IAdminStatsService adminStats)
    {
        _userManager = userManager;
        _adminStats = adminStats;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains(RoleNames.Admin)) return RedirectToAction(nameof(Admin));
        if (roles.Contains(RoleNames.Manager)) return RedirectToAction(nameof(Manager));
        if (roles.Contains(RoleNames.Investigator)) return RedirectToAction(nameof(Investigator));

        return RedirectToAction(nameof(Employee));
    }

    public async Task<IActionResult> Admin()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !await _userManager.IsInRoleAsync(user, RoleNames.Admin))
            return RedirectToAction("AccessDenied", "Account");

        ViewBag.FullName = user.FullName;
        ViewBag.Stats = await _adminStats.GetStatsAsync();
        return View();
    }

    public async Task<IActionResult> Manager()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !await _userManager.IsInRoleAsync(user, RoleNames.Manager))
            return RedirectToAction("AccessDenied", "Account");

        ViewBag.FullName = user.FullName;
        return View();
    }

    public async Task<IActionResult> Investigator()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !await _userManager.IsInRoleAsync(user, RoleNames.Investigator))
            return RedirectToAction("AccessDenied", "Account");

        ViewBag.FullName = user.FullName;
        return View();
    }

    public async Task<IActionResult> Employee()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        ViewBag.FullName = user.FullName;
        return View();
    }
}