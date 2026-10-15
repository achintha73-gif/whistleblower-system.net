using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Entities;

namespace Whistleblowing.Web.ViewComponents;

public class NotificationBadgeViewComponent : ViewComponent
{
    private readonly INotificationService _notifications;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationBadgeViewComponent(
        INotificationService notifications,
        UserManager<ApplicationUser> userManager)
    {
        _notifications = notifications;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Content(string.Empty);

        var count = await _notifications.GetUnreadCountAsync(user.Id);
        if (count == 0) return Content(string.Empty);

        var display = count > 99 ? "99+" : count.ToString();
        return View("Default", display);
    }
}
