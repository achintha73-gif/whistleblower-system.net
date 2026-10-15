using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Whistleblowing.Infrastructure.Data;

namespace Whistleblowing.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LogsController : Controller
    {
        private readonly AppDbContext _context;

        public LogsController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var logs = await _context.ActivityLogs
                .AsNoTracking()
                .Include(log => log.Actor)
                .OrderByDescending(log => log.CreatedAt)
                .Take(200)
                .ToListAsync();
            return View(logs);
        }
    }
}
