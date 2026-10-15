using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Whistleblowing.Application.DTOs;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Domain.Enums;
using Whistleblowing.Infrastructure.Data;
using Whistleblowing.Web.Services;

namespace Whistleblowing.Web.Controllers;

[Authorize(Roles = "Manager,Investigator,Admin")]
public class CasesController : Controller
{
    private readonly ICaseService _cases;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly INotificationService _notifications;
    private readonly IAccountEmailSender _emailSender;

    public CasesController(ICaseService cases, UserManager<ApplicationUser> userManager, AppDbContext context, IWebHostEnvironment environment, INotificationService notifications, IAccountEmailSender emailSender)
    {
        _cases = cases;
        _userManager = userManager;
        _context = context;
        _environment = environment;
        _notifications = notifications;
        _emailSender = emailSender;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var roles = await _userManager.GetRolesAsync(user);
        var isAdmin = roles.Contains("Admin");
        var isManager = roles.Contains("Manager");
        var isInvestigator = roles.Contains("Investigator") && !isAdmin && !isManager;

        // Determine primary role for filtering
        string primaryRole = isAdmin ? RoleNames.Admin
                          : isManager ? RoleNames.Manager
                          : RoleNames.Investigator;

        var cases = await _cases.GetCasesForUserAsync(user.Id, primaryRole);

        string pageTitle = isInvestigator ? "My Cases" : "All Cases";
        string pageSubtitle = isInvestigator
            ? "Cases assigned to you for investigation."
            : "All cases created from approved concerns.";

        ViewBag.PageTitle = pageTitle;
        ViewBag.PageSubtitle = pageSubtitle;
        ViewBag.IsManager = isManager;
        ViewBag.IsAdmin = isAdmin;
        ViewBag.IsInvestigator = isInvestigator;

        return View(cases);
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var roles = await _userManager.GetRolesAsync(user);
        var isAdmin = roles.Contains("Admin");
        var isManager = roles.Contains("Manager");
        var isInvestigator = roles.Contains("Investigator") && !isAdmin && !isManager;

        string primaryRole = isAdmin ? RoleNames.Admin
                          : isManager ? RoleNames.Manager
                          : RoleNames.Investigator;

        var canAccess = await _cases.CanAccessAsync(id, user.Id, primaryRole);
        if (!canAccess) return RedirectToAction("AccessDenied", "Account");

        var caseDetail = await _cases.GetByIdAsync(id);
        if (caseDetail == null) return NotFound();

        ViewBag.IsManager = isManager || isAdmin; // Admin gets manager privileges
        ViewBag.IsAdmin = isAdmin;
        ViewBag.IsInvestigator = isInvestigator || isAdmin; // Admin gets investigator privileges
        ViewBag.CanSubmitReport = isInvestigator || isAdmin;
        ViewBag.CanSubmitForReview = roles.Contains("Investigator") && !isAdmin;
        ViewBag.CanUploadEvidence = isInvestigator || isManager || isAdmin;
        ViewBag.MaxEvidenceSizeMb = await GetEvidenceMaxFileSizeMbAsync();

        return View(caseDetail);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadEvidence(Guid id, IFormFile? file, string? description)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        var role = await GetPrimaryRoleAsync(user);
        if (!await _cases.CanAccessAsync(id, user.Id, role)) return Forbid();
        var maxSizeMb = await GetEvidenceMaxFileSizeMbAsync();
        if (file == null || file.Length == 0 || file.Length > (long)maxSizeMb * 1024 * 1024 || (description?.Length ?? 0) > 500)
        {
            TempData["Error"] = $"Choose a file no larger than {maxSizeMb} MB and keep its description under 500 characters.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedTypes = new Dictionary<string, string> { [".pdf"] = "application/pdf", [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".txt"] = "text/plain" };
        if (!allowedTypes.TryGetValue(extension, out var contentType) || !string.Equals(file.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only PDF, PNG, JPEG, or plain text evidence files are accepted.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        var directory = Path.Combine(_environment.ContentRootPath, "App_Data", "Evidence");
        Directory.CreateDirectory(directory);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(directory, storedName);
        try
        {
            await using (var stream = System.IO.File.Create(path)) await file.CopyToAsync(stream);
            _context.Evidence.Add(new Evidence { FileName = Path.GetFileName(file.FileName), FileType = contentType, FilePath = storedName, FileSize = file.Length, Description = description?.Trim(), CaseId = id, UploadedById = user.Id });
            _context.ActivityLogs.Add(new ActivityLog { EventType = "EvidenceUploaded", Description = "Evidence was uploaded for an investigation case.", EntityType = "Case", EntityId = id, ActorId = user.Id });
            await _context.SaveChangesAsync();
        }
        catch
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            throw;
        }
        TempData["Success"] = "Evidence uploaded successfully.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadEvidence(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        var role = await GetPrimaryRoleAsync(user);
        var evidence = await _context.Evidence.FirstOrDefaultAsync(e => e.Id == id && (e.CaseId != null || e.ComplaintId != null));
        if (evidence?.FilePath == null) return NotFound();

        var caseId = evidence.CaseId ?? await _context.Cases
            .Where(c => c.ComplaintId == evidence.ComplaintId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();
        if (caseId == null) return NotFound();
        if (!await _cases.CanAccessAsync(caseId.Value, user.Id, role)) return Forbid();
        var path = Path.Combine(_environment.ContentRootPath, "App_Data", "Evidence", Path.GetFileName(evidence.FilePath));
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, evidence.FileType, evidence.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> Report(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        var role = await GetPrimaryRoleAsync(user);
        if (role != RoleNames.Investigator && role != RoleNames.Admin) return Forbid();
        if (!await _cases.CanAccessAsync(id, user.Id, role)) return Forbid();
        if (!await _context.Cases.AnyAsync(c => c.Id == id && c.Status == CaseStatus.Investigating)) return BadRequest("Reports can only be edited while a case is being investigated.");
        var report = await _context.InvestigationReports.AsNoTracking().FirstOrDefaultAsync(r => r.CaseId == id);
        return View(new InvestigationReportFormModel { CaseId = id, Findings = report?.Findings ?? string.Empty, Recommendation = report?.Recommendation ?? string.Empty });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(InvestigationReportFormModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        var role = await GetPrimaryRoleAsync(user);
        if (role != RoleNames.Investigator && role != RoleNames.Admin) return Forbid();
        if (!await _cases.CanAccessAsync(model.CaseId, user.Id, role)) return Forbid();
        if (!await _context.Cases.AnyAsync(c => c.Id == model.CaseId && c.Status == CaseStatus.Investigating)) return BadRequest("Reports can only be edited while a case is being investigated.");
        if (!ModelState.IsValid) return View(model);
        var report = await _context.InvestigationReports.FirstOrDefaultAsync(r => r.CaseId == model.CaseId);
        if (report == null)
        {
            report = new InvestigationReport { CaseId = model.CaseId };
            _context.InvestigationReports.Add(report);
        }
        report.Findings = model.Findings.Trim();
        report.Recommendation = model.Recommendation.Trim();
        _context.ActivityLogs.Add(new ActivityLog { EventType = "InvestigationReportSubmitted", Description = "An investigation report was submitted or updated.", EntityType = "Case", EntityId = model.CaseId, ActorId = user.Id });
        await _context.SaveChangesAsync();
        TempData["Success"] = "Investigation report saved.";
        return RedirectToAction(nameof(Detail), new { id = model.CaseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitForReview(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        var role = await GetPrimaryRoleAsync(user);
        if (role != RoleNames.Investigator || !await _cases.CanAccessAsync(id, user.Id, role)) return Forbid();
        var caseRow = await _context.Cases.Include(c => c.InvestigationReport).Include(c => c.Complaint).FirstOrDefaultAsync(c => c.Id == id);
        if (caseRow == null) return NotFound();
        if (caseRow.Status != CaseStatus.Investigating || caseRow.InvestigationReport == null)
        {
            TempData["Error"] = "Submit an investigation report before requesting review.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        caseRow.Status = CaseStatus.PendingReview;
        _context.CaseStatusHistories.Add(new CaseStatusHistory { CaseId = id, Status = CaseStatus.PendingReview, Note = "Investigator submitted the case for review.", ChangedById = user.Id });
        _context.ActivityLogs.Add(new ActivityLog { EventType = "CaseSubmittedForReview", Description = "A case was submitted for manager review.", EntityType = "Case", EntityId = id, ActorId = user.Id });
        await _context.SaveChangesAsync();
        var managerIds = await _userManager.GetUsersInRoleAsync(RoleNames.Manager);
        var managers = managerIds.Where(manager => !manager.IsDeleted && manager.Status == UserStatus.Active && !string.IsNullOrWhiteSpace(manager.Email));
        var caseUrl = Url.Action(nameof(Detail), "Cases", new { id }, Request.Scheme);
        if (!string.IsNullOrWhiteSpace(caseUrl))
        {
            foreach (var manager in managers)
                await _emailSender.SendReportSubmittedAsync(manager.Email!, manager.FullName, caseRow.Complaint.Title, caseUrl, HttpContext.RequestAborted);
        }
        TempData["Success"] = "Case submitted for manager review.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, Authorize(Roles = "Manager,Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid id, string? note)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        var caseRow = await _context.Cases.Include(c => c.Investigators).Include(c => c.Complaint).FirstOrDefaultAsync(c => c.Id == id);
        if (caseRow == null) return NotFound();
        if (caseRow.Status != CaseStatus.PendingReview)
        {
            TempData["Error"] = "Only cases awaiting review can be closed.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        caseRow.Status = CaseStatus.Closed;
        caseRow.ClosedAt = DateTime.UtcNow;
        _context.CaseStatusHistories.Add(new CaseStatusHistory { CaseId = id, Status = CaseStatus.Closed, Note = string.IsNullOrWhiteSpace(note) ? "Closed after manager review." : note.Trim()[..Math.Min(note.Trim().Length, 1000)], ChangedById = user.Id });
        _context.ActivityLogs.Add(new ActivityLog { EventType = "CaseClosed", Description = "A case was closed after review.", EntityType = "Case", EntityId = id, ActorId = user.Id });
        await _context.SaveChangesAsync();
        await _notifications.NotifyManyAsync(caseRow.Investigators.Select(i => i.InvestigatorId), $"Case '{caseRow.Complaint.Title}' has been closed after manager review.");
        TempData["Success"] = "Case closed.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, Authorize(Roles = "Manager,Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnForInvestigation(Guid id, string? note)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        var caseRow = await _context.Cases.Include(c => c.Investigators).Include(c => c.Complaint).FirstOrDefaultAsync(c => c.Id == id);
        if (caseRow == null) return NotFound();
        if (caseRow.Status != CaseStatus.PendingReview)
        {
            TempData["Error"] = "Only cases awaiting review can be returned to investigation.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        caseRow.Status = CaseStatus.Investigating;
        _context.CaseStatusHistories.Add(new CaseStatusHistory { CaseId = id, Status = CaseStatus.Investigating, Note = string.IsNullOrWhiteSpace(note) ? "Returned for further investigation." : note.Trim()[..Math.Min(note.Trim().Length, 1000)], ChangedById = user.Id });
        _context.ActivityLogs.Add(new ActivityLog { EventType = "CaseReturnedForInvestigation", Description = "A case was returned for further investigation.", EntityType = "Case", EntityId = id, ActorId = user.Id });
        await _context.SaveChangesAsync();
        await _notifications.NotifyManyAsync(caseRow.Investigators.Select(i => i.InvestigatorId), $"Case '{caseRow.Complaint.Title}' was returned for further investigation. {note?.Trim()}");
        TempData["Success"] = "Case returned to investigation.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    private async Task<string> GetPrimaryRoleAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.Contains(RoleNames.Admin) ? RoleNames.Admin : roles.Contains(RoleNames.Manager) ? RoleNames.Manager : roles.Contains(RoleNames.Investigator) ? RoleNames.Investigator : RoleNames.Employee;
    }

    private async Task<int> GetEvidenceMaxFileSizeMbAsync()
    {
        var value = await _context.SystemSettings.AsNoTracking()
            .Where(setting => setting.Key == ApplicationSettingKeys.EvidenceMaxFileSizeMb)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync();
        return int.TryParse(value, out var size) && size >= 1 && size <= 25 ? size : 10;
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpGet]
    public async Task<IActionResult> Convert(Guid complaintId)
    {
        var preview = await _cases.GetConversionPreviewAsync(complaintId);
        if (preview == null)
        {
            TempData["Error"] = "Complaint not found or is not pending.";
            return RedirectToAction("Index", "Complaints");
        }
        return View(preview);
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(ConvertComplaintDto model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var result = await _cases.ConvertFromComplaintAsync(
            model.ComplaintId, model.Priority, user.Id);

        if (!result.Success)
        {
            TempData["Error"] = string.Join(", ", result.Errors ?? new[] { "Conversion failed" });
            return RedirectToAction("Convert", new { complaintId = model.ComplaintId });
        }

        TempData["Success"] = "Case created successfully";
        return RedirectToAction(nameof(Detail), new { id = result.CaseId });
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpGet]
    public async Task<IActionResult> Assign(Guid id)
    {
        var preview = await _cases.GetAssignPreviewAsync(id);
        if (preview == null) return NotFound();
        return View(preview);
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(Guid caseId, List<Guid> investigatorIds)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var result = await _cases.AssignInvestigatorsAsync(
            caseId, investigatorIds ?? new List<Guid>(), user.Id);

        if (!result.Success)
        {
            TempData["Error"] = string.Join(", ", result.Errors ?? new[] { "Assignment failed" });
            return RedirectToAction(nameof(Assign), new { id = caseId });
        }

        if (result.Added?.Count > 0)
        {
            var caseDetails = await _cases.GetByIdAsync(caseId);
            var caseUrl = Url.Action(nameof(Detail), "Cases", new { id = caseId }, Request.Scheme);
            if (caseDetails != null && !string.IsNullOrWhiteSpace(caseUrl))
            {
                foreach (var investigatorId in result.Added)
                {
                    var investigator = await _userManager.FindByIdAsync(investigatorId.ToString());
                    if (investigator is { IsDeleted: false, Status: UserStatus.Active } && !string.IsNullOrWhiteSpace(investigator.Email))
                        await _emailSender.SendCaseAssignedAsync(investigator.Email, investigator.FullName, caseDetails.ComplaintTitle, caseUrl, HttpContext.RequestAborted);
                }
            }
        }

        TempData["Success"] = "Investigators updated successfully";
        return RedirectToAction(nameof(Detail), new { id = caseId });
    }
}


public class InvestigationReportFormModel
{
    public Guid CaseId { get; set; }
    [Required, StringLength(10000, MinimumLength = 20)] public string Findings { get; set; } = string.Empty;
    [Required, StringLength(5000, MinimumLength = 10)] public string Recommendation { get; set; } = string.Empty;
}
