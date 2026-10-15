using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Whistleblowing.Infrastructure.Data;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Whistleblowing.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : Controller
    {
        private readonly AppDbContext _context;

        public DepartmentsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                .Include(d => d.Manager)
                .Include(d => d.Users.Where(u => !u.IsDeleted))
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync();
            return View(departments);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadManagersAsync();
            return View("Edit", new DepartmentFormModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentFormModel model)
        {
            if (model.ManagerId.HasValue && !await IsActiveManagerAsync(model.ManagerId.Value))
                ModelState.AddModelError(nameof(model.ManagerId), "Choose an active user with the Manager role.");
            if (!ModelState.IsValid) { await LoadManagersAsync(); return View("Edit", model); }
            var name = model.Name.Trim();
            if (await _context.Departments.AnyAsync(d => d.Name == name))
            {
                ModelState.AddModelError(nameof(model.Name), "A department with this name already exists.");
                await LoadManagersAsync();
                return View("Edit", model);
            }
            var department = new Department { Name = name, Description = model.Description?.Trim(), ManagerId = model.ManagerId };
            _context.Departments.Add(department);
            AddAudit("DepartmentCreated", "A department was created.", department.Id);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Department created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound();
            await LoadManagersAsync();
            return View(new DepartmentFormModel { Id = department.Id, Name = department.Name, Description = department.Description, ManagerId = department.ManagerId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DepartmentFormModel model)
        {
            var department = await _context.Departments.FindAsync(model.Id);
            if (department == null) return NotFound();
            if (model.ManagerId.HasValue && !await IsActiveManagerAsync(model.ManagerId.Value))
                ModelState.AddModelError(nameof(model.ManagerId), "Choose an active user with the Manager role.");
            if (ModelState.IsValid && await _context.Departments.AnyAsync(d => d.Id != model.Id && d.Name == model.Name.Trim()))
                ModelState.AddModelError(nameof(model.Name), "A department with this name already exists.");
            if (!ModelState.IsValid) { await LoadManagersAsync(); return View(model); }
            department.Name = model.Name.Trim();
            department.Description = model.Description?.Trim();
            department.ManagerId = model.ManagerId;
            AddAudit("DepartmentUpdated", "A department was updated.", department.Id);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Department updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound();
            _context.Departments.Remove(department);
            AddAudit("DepartmentDeleted", "A department was deleted.", department.Id);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Department deleted. Its users are now unassigned.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadManagersAsync()
        {
            var managerRoleId = await _context.Roles
                .Where(r => r.Name == RoleNames.Manager)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();
            ViewBag.Managers = await _context.Users.AsNoTracking()
                .Where(u => !u.IsDeleted && u.Status == UserStatus.Active && _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == managerRoleId))
                .OrderBy(u => u.FullName)
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(u.FullName, u.Id.ToString()))
                .ToListAsync();
        }

        private async Task<bool> IsActiveManagerAsync(Guid userId)
        {
            var managerRoleId = await _context.Roles
                .Where(role => role.Name == RoleNames.Manager)
                .Select(role => role.Id)
                .FirstOrDefaultAsync();
            return managerRoleId != Guid.Empty && await _context.Users.AnyAsync(user =>
                user.Id == userId && !user.IsDeleted && user.Status == UserStatus.Active &&
                _context.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RoleId == managerRoleId));
        }

        private void AddAudit(string eventType, string description, Guid entityId)
        {
            var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _context.ActivityLogs.Add(new ActivityLog { EventType = eventType, Description = description, EntityType = "Department", EntityId = entityId, ActorId = Guid.TryParse(actor, out var id) ? id : null });
        }
    }

    public class DepartmentFormModel
    {
        public Guid Id { get; set; }
        [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
        [StringLength(1000)] public string? Description { get; set; }
        public Guid? ManagerId { get; set; }
    }
}
