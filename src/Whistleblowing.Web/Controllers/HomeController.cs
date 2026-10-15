using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Whistleblowing.Domain.Enums;
using Whistleblowing.Infrastructure.Data;

namespace Whistleblowing.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context) => _context = context;

    public IActionResult Index()
    {
        // Redirect root to dashboard (role-based)
        return RedirectToAction("Index", "Dashboard");
    }

    public async Task<IActionResult> Support()
    {
        var supportSettings = await _context.SystemSettings.AsNoTracking()
            .Where(setting => setting.Key == ApplicationSettingKeys.SupportPhone || setting.Key == ApplicationSettingKeys.SupportEmail)
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value);

        ViewBag.SupportPhone = supportSettings.GetValueOrDefault(ApplicationSettingKeys.SupportPhone);
        ViewBag.SupportEmail = supportSettings.GetValueOrDefault(ApplicationSettingKeys.SupportEmail);
        return View();
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    public IActionResult Error() => View();
}
