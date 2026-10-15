using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Entities;

namespace Whistleblowing.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notifications;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(
        INotificationService notifications,
        UserManager<ApplicationUser> userManager)
    {
        _notifications = notifications;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var list = await _notifications.GetForUserAsync(user.Id);
        ViewBag.UnreadCount = list.Count(n => !n.IsRead);
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        await _notifications.MarkAsReadAsync(id, user.Id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        await _notifications.MarkAllAsReadAsync(user.Id);
        return RedirectToAction(nameof(Index));
    }
}
