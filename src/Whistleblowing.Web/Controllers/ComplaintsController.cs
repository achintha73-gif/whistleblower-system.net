using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security.Claims;
using Whistleblowing.Application.DTOs;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Domain.Enums;
using Whistleblowing.Infrastructure.Data;
using Whistleblowing.Web.Models.Complaints;
using Whistleblowing.Web.Services;

namespace Whistleblowing.Web.Controllers;

[Authorize]
public class ComplaintsController : Controller
{
    private readonly IComplaintService _complaints;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly INotificationService _notifications;
    private readonly EvidenceUploadService _evidenceUploads;

    public ComplaintsController(IComplaintService complaints, UserManager<ApplicationUser> userManager, AppDbContext context, IWebHostEnvironment environment, INotificationService notifications, EvidenceUploadService evidenceUploads)
    {
        _complaints = complaints;
        _userManager = userManager;
        _context = context;
        _environment = environment;
        _notifications = notifications;
        _evidenceUploads = evidenceUploads;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var roles = await _userManager.GetRolesAsync(user);

        // Investigator only: redirect to cases
        if (roles.Contains("Investigator") && !roles.Contains("Manager") && !roles.Contains("Admin"))
            return RedirectToAction("Index", "Cases");

        var isEmployee = roles.Contains("Employee") && !roles.Contains("Manager") && !roles.Contains("Admin");
        var isAdmin = roles.Contains("Admin");
        var isManager = roles.Contains("Manager");

        IReadOnlyList<ComplaintDto> complaints;
        if (isEmployee)
        {
            // Employee: own only
            complaints = await _complaints.GetMyComplaintsAsync(user.Id);
        }
        else if (isManager || isAdmin)
        {
            // Manager + Admin: all
            complaints = await _complaints.GetAllComplaintsAsync();
        }
        else
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        ViewBag.IsEmployee = isEmployee;
        ViewBag.IsAdmin = isAdmin;
        ViewBag.IsManager = isManager;
        ViewBag.CanSubmit = isEmployee || isAdmin; // Admin can submit
        ViewBag.CanReview = isManager || isAdmin;  // Manager + Admin can convert
        ViewBag.PageTitle = (isEmployee && !isAdmin) ? "My Concerns" : "All Concerns";
        ViewBag.PageSubtitle = (isEmployee && !isAdmin)
            ? "Concerns you have submitted."
            : "All concerns submitted in the system.";

        return View(complaints);
    }

    [HttpGet]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = ComplaintCategories.All;
        ViewBag.MaxEvidenceSizeMb = await GetEvidenceMaxFileSizeMbAsync();
        return View(new CreateComplaintDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> Create(CreateComplaintDto model, List<IFormFile>? evidenceFiles, string? evidenceDescription, CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (!string.IsNullOrWhiteSpace(model.Category) && !ComplaintCategories.All.Contains(model.Category, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.Category), "Choose a category from the list.");

        var maxSizeMb = await GetEvidenceMaxFileSizeMbAsync();
        var evidenceError = await _evidenceUploads.ValidateAsync(evidenceFiles, maxSizeMb, ct);
        if (evidenceError != null) ModelState.AddModelError(nameof(evidenceFiles), evidenceError);
        if ((evidenceDescription?.Length ?? 0) > 500) ModelState.AddModelError(nameof(evidenceDescription), "Evidence description must be 500 characters or fewer.");

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = ComplaintCategories.All;
            ViewBag.MaxEvidenceSizeMb = maxSizeMb;
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var created = await _complaints.CreateAsync(user.Id, model, ct);
        await _evidenceUploads.SaveAsync(evidenceFiles, created.Id, model.IsAnonymous ? null : user.Id, evidenceDescription, model.IsAnonymous ? null : user.Id, ct);
        await transaction.CommitAsync(ct);
        if (model.IsAnonymous)
            return RedirectToAction("Success", "AnonymousComplaints", new { code = created.ReferenceCode });
        TempData["Success"] = "Concern submitted successfully";
        return RedirectToAction(nameof(Detail), new { id = created.Id });
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var complaint = await _complaints.GetByIdAsync(id);
        if (complaint == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        // Investigator only: no access
        if (roles.Contains("Investigator") && !roles.Contains("Manager") && !roles.Contains("Admin"))
            return RedirectToAction("AccessDenied", "Account");

        var isAdmin = roles.Contains("Admin");
        var isManager = roles.Contains("Manager");
        var isEmployee = roles.Contains("Employee") && !roles.Contains("Manager") && !roles.Contains("Admin");

        // Employee can only see own
        if (isEmployee && complaint.UserId != user.Id)
            return RedirectToAction("AccessDenied", "Account");

        ViewBag.IsAdmin = isAdmin;
        ViewBag.IsManager = isManager;
        ViewBag.IsEmployee = isEmployee;
        ViewBag.CanApprove = isManager || isAdmin;  // Manager + Admin
        ViewBag.CanConvert = (isManager || isAdmin) && (complaint.Status == ComplaintStatus.Pending || complaint.Status == ComplaintStatus.Approved);
        ViewBag.CanReview = (isManager || isAdmin) && (complaint.Status == ComplaintStatus.Pending || complaint.Status == ComplaintStatus.UnderReview);
        ViewBag.CanEdit = (isEmployee && complaint.UserId == user.Id) || isAdmin;
        ViewBag.CanAddInformation = isEmployee && complaint.UserId == user.Id;
        ViewBag.CanUploadEvidence = ViewBag.CanAddInformation || isAdmin || isManager;
        if (isEmployee && complaint.UserId == user.Id && !complaint.IsAnonymous)
        {
            var reportCaseStatus = await _context.Cases.AsNoTracking()
                .Where(c => c.ComplaintId == id &&
                    (c.Status == CaseStatus.PendingReview || c.Status == CaseStatus.Closed || c.Status == CaseStatus.Archived) &&
                    c.InvestigationReport != null)
                .Select(c => (CaseStatus?)c.Status)
                .FirstOrDefaultAsync();
            ViewBag.CanViewInvestigationReport = reportCaseStatus.HasValue;
            ViewBag.ReportManagerReviewed = reportCaseStatus == CaseStatus.Closed || reportCaseStatus == CaseStatus.Archived;
        }
        ViewBag.MaxEvidenceSizeMb = await GetEvidenceMaxFileSizeMbAsync();

        return View(complaint);
    }

    [HttpGet]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> InvestigationReport(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var report = await _context.Cases.AsNoTracking()
            .Where(c => c.ComplaintId == id && c.Complaint.UserId == user.Id && !c.Complaint.IsAnonymous &&
                (c.Status == CaseStatus.PendingReview || c.Status == CaseStatus.Closed || c.Status == CaseStatus.Archived) &&
                c.InvestigationReport != null)
            .Select(c => new EmployeeInvestigationReportViewModel
            {
                ComplaintId = id,
                CaseId = c.Id,
                ConcernTitle = c.Complaint.Title,
                Category = c.Complaint.Category,
                Findings = c.InvestigationReport!.Findings,
                Recommendation = c.InvestigationReport.Recommendation,
                CompletedAt = c.InvestigationReport.UpdatedAt ?? c.InvestigationReport.CreatedAt,
                IsManagerReviewed = c.Status == CaseStatus.Closed || c.Status == CaseStatus.Archived,
            })
            .FirstOrDefaultAsync();

        return report == null ? NotFound() : View("InvestigationReport", report);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddInformation(Guid id, string title, string description, InformationType informationType)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.Id == id);
        if (complaint == null) return NotFound();
        var isOwner = complaint.UserId == user.Id && await _userManager.IsInRoleAsync(user, RoleNames.Employee);
        if (!ModelState.IsValid || !isOwner || string.IsNullOrWhiteSpace(title) || title.Length > 200 || string.IsNullOrWhiteSpace(description) || description.Length > 2000 || !Enum.IsDefined(informationType))
        {
            TempData["Error"] = "Please provide valid additional information.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        _context.AdditionalInformation.Add(new AdditionalInformation { ComplaintId = id, Title = title.Trim(), Description = description.Trim(), InformationType = informationType, SubmittedById = user.Id });
        _context.ActivityLogs.Add(new ActivityLog { EventType = "AdditionalInformationSubmitted", Description = "Additional information was submitted for a concern.", EntityType = "Complaint", EntityId = id, ActorId = user.Id });
        await _context.SaveChangesAsync();
        TempData["Success"] = "Additional information submitted.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadEvidence(Guid id, List<IFormFile>? evidenceFiles, string? description, CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.Id == id);
        if (complaint == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        var allowed = roles.Contains(RoleNames.Admin) || roles.Contains(RoleNames.Manager) || (roles.Contains(RoleNames.Employee) && complaint.UserId == user.Id);
        if (!allowed) return Forbid();
        var maxSizeMb = await GetEvidenceMaxFileSizeMbAsync();
        var evidenceError = await _evidenceUploads.ValidateAsync(evidenceFiles, maxSizeMb, ct);
        if (evidenceError != null || (description?.Length ?? 0) > 500)
        {
            TempData["Error"] = evidenceError ?? "Keep the evidence description under 500 characters.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        await _evidenceUploads.SaveAsync(evidenceFiles, id, user.Id, description, user.Id, ct);
        TempData["Success"] = "Evidence uploaded successfully.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadEvidence(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        var evidence = await _context.Evidence.Include(e => e.Complaint).FirstOrDefaultAsync(e => e.Id == id && e.ComplaintId != null);
        if (evidence?.Complaint == null || evidence.FilePath == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        var allowed = roles.Contains(RoleNames.Admin) || roles.Contains(RoleNames.Manager) || (roles.Contains(RoleNames.Employee) && evidence.Complaint.UserId == user.Id);
        if (!allowed) return Forbid();
        var safeName = Path.GetFileName(evidence.FilePath);
        var path = Path.Combine(_environment.ContentRootPath, "App_Data", "Evidence", safeName);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, evidence.FileType, evidence.FileName);
    }

    [HttpPost, Authorize(Roles = "Manager,Admin"), ValidateAntiForgeryToken]
    public Task<IActionResult> StartReview(Guid id) => ChangeStatusAsync(id, ComplaintStatus.UnderReview);

    [HttpPost, Authorize(Roles = "Manager,Admin"), ValidateAntiForgeryToken]
    public Task<IActionResult> Approve(Guid id) => ChangeStatusAsync(id, ComplaintStatus.Approved);

    [HttpPost, Authorize(Roles = "Manager,Admin"), ValidateAntiForgeryToken]
    public Task<IActionResult> Reject(Guid id) => ChangeStatusAsync(id, ComplaintStatus.Rejected);

    private async Task<IActionResult> ChangeStatusAsync(Guid id, ComplaintStatus nextStatus)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.Id == id);
        if (complaint == null) return NotFound();
        if (complaint.Status != ComplaintStatus.Pending && complaint.Status != ComplaintStatus.UnderReview)
        {
            TempData["Error"] = "Only pending or under-review concerns can be reviewed.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        complaint.Status = nextStatus;
        var eventType = nextStatus switch
        {
            ComplaintStatus.UnderReview => "ConcernReviewStarted",
            ComplaintStatus.Approved => "ConcernApproved",
            ComplaintStatus.Rejected => "ConcernRejected",
            _ => "ConcernStatusChanged",
        };
        var description = nextStatus switch
        {
            ComplaintStatus.UnderReview => "A concern was moved into review.",
            ComplaintStatus.Approved => "A concern was approved for case conversion.",
            ComplaintStatus.Rejected => "A concern was rejected.",
            _ => "A concern status changed.",
        };
        _context.ActivityLogs.Add(new ActivityLog { EventType = eventType, Description = description, EntityType = "Complaint", EntityId = id, ActorId = user.Id });
        await _context.SaveChangesAsync();
        if (complaint.UserId.HasValue && !complaint.IsAnonymous)
        {
            var notificationMessage = nextStatus switch
            {
                ComplaintStatus.UnderReview => $"Your concern '{complaint.Title}' is now under review.",
                ComplaintStatus.Approved => $"Your concern '{complaint.Title}' has been approved for investigation.",
                ComplaintStatus.Rejected => $"Your concern '{complaint.Title}' has been reviewed and rejected.",
                _ => $"Your concern '{complaint.Title}' status has changed.",
            };
            await _notifications.NotifyAsync(complaint.UserId.Value, notificationMessage);
        }
        TempData["Success"] = nextStatus switch
        {
            ComplaintStatus.UnderReview => "Concern marked under review.",
            ComplaintStatus.Approved => "Concern approved. You can now create an investigation case.",
            _ => "Concern rejected.",
        };
        return RedirectToAction(nameof(Detail), new { id });
    }

    private async Task<int> GetEvidenceMaxFileSizeMbAsync()
    {
        var value = await _context.SystemSettings.AsNoTracking()
            .Where(setting => setting.Key == ApplicationSettingKeys.EvidenceMaxFileSizeMb)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync();
        return int.TryParse(value, out var size) && size >= 1 && size <= 25 ? size : 10;
    }
}
