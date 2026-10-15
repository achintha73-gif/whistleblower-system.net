using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Domain.Enums;
using Whistleblowing.Infrastructure.Data;

namespace Whistleblowing.Web.Controllers;

[Authorize(Roles = "Admin")]
public class SettingsController : Controller
{
    private readonly AppDbContext _context;
    public SettingsController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        await EnsureDefaultsAsync();
        return View(await _context.SystemSettings.AsNoTracking().OrderBy(s => s.Key).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View("Edit", new SettingFormModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SettingFormModel model)
    {
        if (!ModelState.IsValid) return View("Edit", model);
        model.Key = model.Key?.Trim() ?? string.Empty;
        if (!ValidateOption(model)) return View("Edit", model);
        if (await _context.SystemSettings.AnyAsync(s => s.Key == model.Key))
        {
            ModelState.AddModelError(nameof(model.Key), "A setting with this key already exists.");
            return View("Edit", model);
        }
        var setting = new SystemSetting { Key = model.Key, Value = model.Value, Description = model.Description?.Trim() };
        _context.SystemSettings.Add(setting);
        AddAudit("SettingCreated", "A system setting was created.", setting.Id);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Setting created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var setting = await _context.SystemSettings.FindAsync(id);
        return setting == null ? NotFound() : View(new SettingFormModel { Id = setting.Id, Key = setting.Key, Value = setting.Value, Description = setting.Description });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SettingFormModel model)
    {
        var setting = await _context.SystemSettings.FindAsync(model.Id);
        if (setting == null) return NotFound();
        model.Key = model.Key?.Trim() ?? string.Empty;
        if (!ValidateOption(model)) return View(model);
        if (ModelState.IsValid && await _context.SystemSettings.AnyAsync(s => s.Id != model.Id && s.Key == model.Key))
            ModelState.AddModelError(nameof(model.Key), "A setting with this key already exists.");
        if (!ModelState.IsValid) return View(model);
        setting.Key = model.Key;
        setting.Value = model.Value;
        setting.Description = model.Description?.Trim();
        AddAudit("SettingUpdated", "A system setting was updated.", setting.Id);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Setting updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var setting = await _context.SystemSettings.FindAsync(id);
        if (setting == null) return NotFound();
        _context.SystemSettings.Remove(setting);
        AddAudit("SettingDeleted", "A system setting was deleted.", setting.Id);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Setting deleted.";
        return RedirectToAction(nameof(Index));
    }

    private void AddAudit(string eventType, string description, Guid entityId)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _context.ActivityLogs.Add(new ActivityLog { EventType = eventType, Description = description, EntityType = "SystemSetting", EntityId = entityId, ActorId = Guid.TryParse(actor, out var id) ? id : null });
    }

    private async Task EnsureDefaultsAsync()
    {
        foreach (var setting in ApplicationSettingKeys.Defaults)
        {
            if (!await _context.SystemSettings.AnyAsync(s => s.Key == setting.Key))
            {
                _context.SystemSettings.Add(new SystemSetting { Key = setting.Key, Value = setting.Value, Description = setting.Description });
            }
        }

        if (_context.ChangeTracker.HasChanges()) await _context.SaveChangesAsync();
    }

    private bool ValidateOption(SettingFormModel model)
    {
        model.Value = model.Value?.Trim() ?? string.Empty;
        switch (model.Key)
        {
            case ApplicationSettingKeys.SupportPhone when model.Value.Length > 0 &&
                (!Regex.IsMatch(model.Value, @"^[+0-9()\s.-]+$", RegexOptions.None, TimeSpan.FromMilliseconds(100)) || model.Value.Count(char.IsDigit) < 7 || model.Value.Count(char.IsDigit) > 15):
                ModelState.AddModelError(nameof(model.Value), "Enter a valid support phone number with 7 to 15 digits, or leave it blank.");
                return false;
            case ApplicationSettingKeys.SupportEmail when model.Value.Length > 0 &&
                (!MailAddress.TryCreate(model.Value, out var email) || !string.Equals(email.Address, model.Value, StringComparison.OrdinalIgnoreCase)):
                ModelState.AddModelError(nameof(model.Value), "Enter a valid support email address, or leave it blank.");
                return false;
            case ApplicationSettingKeys.OrganizationName when string.IsNullOrWhiteSpace(model.Value) || model.Value.Length > 100:
                ModelState.AddModelError(nameof(model.Value), "Enter an organization name up to 100 characters.");
                return false;
            case ApplicationSettingKeys.AnonymousSubmissionsEnabled when !bool.TryParse(model.Value, out _):
                ModelState.AddModelError(nameof(model.Value), "Choose Yes or No.");
                return false;
            case ApplicationSettingKeys.EvidenceMaxFileSizeMb when !int.TryParse(model.Value, out var size) || size < 1 || size > 25:
                ModelState.AddModelError(nameof(model.Value), "Choose a file size limit from 1 to 25 MB.");
                return false;
            case ApplicationSettingKeys.DefaultCasePriority when !Enum.TryParse<CasePriority>(model.Value, true, out var priority) || !Enum.IsDefined(priority):
                ModelState.AddModelError(nameof(model.Value), "Choose a valid default case priority.");
                return false;
            default:
                if (string.IsNullOrWhiteSpace(model.Value) && model.Key != ApplicationSettingKeys.SupportPhone && model.Key != ApplicationSettingKeys.SupportEmail)
                {
                    ModelState.AddModelError(nameof(model.Value), "A value is required.");
                    return false;
                }
                return true;
        }
    }
}

public class SettingFormModel
{
    public Guid Id { get; set; }
    [Required, StringLength(100)] public string Key { get; set; } = string.Empty;
    [StringLength(10000)] public string Value { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
}
