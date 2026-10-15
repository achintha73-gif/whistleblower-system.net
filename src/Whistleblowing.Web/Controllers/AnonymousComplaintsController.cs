using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Whistleblowing.Application.DTOs;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Enums;
using Whistleblowing.Infrastructure.Data;
using Whistleblowing.Web.Services;
using System.IO;

namespace Whistleblowing.Web.Controllers;

[AllowAnonymous]
public class AnonymousComplaintsController : Controller
{
    private readonly IComplaintService _complaintService;
    private readonly ILogger<AnonymousComplaintsController> _logger;
    private readonly AppDbContext _context;
    private readonly EvidenceUploadService _evidenceUploads;
    private readonly IWebHostEnvironment _environment;
    private readonly IRecaptchaVerifier _recaptchaVerifier;
    private readonly IConfiguration _configuration;

    public AnonymousComplaintsController(IComplaintService complaintService, ILogger<AnonymousComplaintsController> logger, AppDbContext context, EvidenceUploadService evidenceUploads, IWebHostEnvironment environment, IRecaptchaVerifier recaptchaVerifier, IConfiguration configuration)
    {
        _complaintService = complaintService;
        _logger = logger;
        _context = context;
        _evidenceUploads = evidenceUploads;
        _environment = environment;
        _recaptchaVerifier = recaptchaVerifier;
        _configuration = configuration;
    }

    // -------------------------------------------------------------
    // GET: Submit form
    // -------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Portal()
    {
        ViewBag.AnonymousEnabled = await IsAnonymousEnabledAsync();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Submit()
    {
        ViewBag.AnonymousEnabled = await IsAnonymousEnabledAsync();
        ViewBag.MaxEvidenceSizeMb = await GetEvidenceMaxFileSizeMbAsync();
        SetRecaptchaViewData();
        return View(new SubmitAnonymousComplaintDto());
    }

    // -------------------------------------------------------------
    // POST: Submit complaint
    // -------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("anonymous-tracking")]
    public async Task<IActionResult> Submit(SubmitAnonymousComplaintDto model, List<IFormFile>? evidenceFiles, string? evidenceDescription, [FromForm(Name = "g-recaptcha-response")] string? recaptchaToken, CancellationToken ct)
    {
        if (!await IsAnonymousEnabledAsync())
        {
            ViewBag.AnonymousEnabled = false;
            ViewBag.MaxEvidenceSizeMb = await GetEvidenceMaxFileSizeMbAsync();
            SetRecaptchaViewData();
            return View(model);
        }
        if (!string.IsNullOrWhiteSpace(model.Category) && !ComplaintCategories.All.Contains(model.Category, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.Category), "Choose a category from the list.");
        var maxSizeMb = await GetEvidenceMaxFileSizeMbAsync();
        var evidenceError = await _evidenceUploads.ValidateAsync(evidenceFiles, maxSizeMb, ct);
        if (evidenceError != null) ModelState.AddModelError(nameof(evidenceFiles), evidenceError);
        if ((evidenceDescription?.Length ?? 0) > 500) ModelState.AddModelError(nameof(evidenceDescription), "Evidence description must be 500 characters or fewer.");
        if (!ModelState.IsValid)
        {
            ViewBag.AnonymousEnabled = true;
            ViewBag.MaxEvidenceSizeMb = maxSizeMb;
            SetRecaptchaViewData();
            return View(model);
        }

        if (!await _recaptchaVerifier.VerifyAsync(recaptchaToken, ct))
        {
            ModelState.AddModelError(string.Empty, "Please complete the reCAPTCHA check before submitting.");
            ViewBag.AnonymousEnabled = true;
            ViewBag.MaxEvidenceSizeMb = maxSizeMb;
            SetRecaptchaViewData();
            return View(model);
        }

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            var result = await _complaintService.SubmitAnonymousAsync(model, ct);
            await _evidenceUploads.SaveAsync(evidenceFiles, result.Id, null, evidenceDescription, null, ct);
            await transaction.CommitAsync(ct);
            return RedirectToAction(nameof(Success), new { code = result.ReferenceCode });
        }
        catch (Exception ex)
        {
            ViewBag.AnonymousEnabled = true;
            ViewBag.MaxEvidenceSizeMb = maxSizeMb;
            SetRecaptchaViewData();
            ModelState.AddModelError(string.Empty, "Failed to submit complaint. Please try again.");
            _logger.LogError(ex, "Anonymous concern submission failed.");
            return View(model);
        }
    }

    private async Task<bool> IsAnonymousEnabledAsync()
    {
        var value = await _context.SystemSettings.AsNoTracking()
            .Where(setting => setting.Key == ApplicationSettingKeys.AnonymousSubmissionsEnabled)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync();
        return !bool.TryParse(value, out var enabled) || enabled;
    }

    // -------------------------------------------------------------
    // GET: Success page
    // -------------------------------------------------------------
    [HttpGet]
    public IActionResult Success(string code)
    {
        if (string.IsNullOrEmpty(code))
            return RedirectToAction(nameof(Submit));

        ViewData["ReferenceCode"] = code;
        return View();
    }

    // -------------------------------------------------------------
    // GET: Track form
    // -------------------------------------------------------------
    [HttpGet]
    public IActionResult Track(string? code = null)
    {
        return View(new TrackComplaintDto { ReferenceCode = code ?? string.Empty });
    }

    // -------------------------------------------------------------
    // POST: Track complaint
    // -------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("anonymous-tracking")]
    public async Task<IActionResult> Track(TrackComplaintDto model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var complaint = await _complaintService.TrackByReferenceCodeAsync(model.ReferenceCode, ct);
        if (complaint == null)
        {
            ModelState.AddModelError(string.Empty, "No complaint found with that reference code.");
            return View(model);
        }

        return RedirectToAction(nameof(View), new { code = complaint.ReferenceCode });
    }

    // -------------------------------------------------------------
    // GET: View tracked complaint
    // -------------------------------------------------------------
    [HttpGet]
    [EnableRateLimiting("anonymous-tracking")]
    public async Task<IActionResult> View(string code, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(code))
            return RedirectToAction(nameof(Track));

        var complaint = await _complaintService.TrackByReferenceCodeAsync(code, ct);
        if (complaint == null)
            return RedirectToAction(nameof(Track));

        var infos = await _complaintService.GetAnonymousInfoAsync(code, ct);
        ViewData["AdditionalInfo"] = infos;
        var evidence = await _context.Evidence.AsNoTracking()
            .Where(item => item.ComplaintId == complaint.Id)
            .OrderByDescending(item => item.UploadedAt)
            .Select(item => new EvidenceItemDto { Id = item.Id, FileName = item.FileName, Description = item.Description, UploadedAt = item.UploadedAt })
            .ToListAsync(ct);
        ViewData["Evidence"] = evidence;
        ViewData["MaxEvidenceSizeMb"] = await GetEvidenceMaxFileSizeMbAsync();

        return View(complaint);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("anonymous-tracking")]
    public async Task<IActionResult> AddEvidence(string referenceCode, List<IFormFile>? evidenceFiles, string? description, CancellationToken ct)
    {
        var normalizedCode = referenceCode?.Trim().ToUpperInvariant() ?? string.Empty;
        var complaint = await _context.Complaints.FirstOrDefaultAsync(item => item.ReferenceCode == normalizedCode && item.IsAnonymous, ct);
        if (complaint == null) return NotFound();

        var maxSizeMb = await GetEvidenceMaxFileSizeMbAsync();
        var evidenceError = await _evidenceUploads.ValidateAsync(evidenceFiles, maxSizeMb, ct);
        if (evidenceError != null || (description?.Length ?? 0) > 500)
        {
            TempData["Error"] = evidenceError ?? "Keep the evidence description under 500 characters.";
            return RedirectToAction(nameof(View), new { code = normalizedCode });
        }

        await _evidenceUploads.SaveAsync(evidenceFiles, complaint.Id, null, description, null, ct);
        TempData["Success"] = "Evidence uploaded successfully.";
        return RedirectToAction(nameof(View), new { code = normalizedCode });
    }

    [HttpGet]
    [EnableRateLimiting("anonymous-tracking")]
    public async Task<IActionResult> DownloadEvidence(string code, Guid id, CancellationToken ct)
    {
        var normalizedCode = code?.Trim().ToUpperInvariant() ?? string.Empty;
        var evidence = await _context.Evidence.AsNoTracking()
            .Include(item => item.Complaint)
            .FirstOrDefaultAsync(item => item.Id == id && item.Complaint != null && item.Complaint.IsAnonymous && item.Complaint.ReferenceCode == normalizedCode, ct);
        if (evidence?.FilePath == null) return NotFound();

        var path = Path.Combine(_environment.ContentRootPath, "App_Data", "Evidence", Path.GetFileName(evidence.FilePath));
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, evidence.FileType, evidence.FileName);
    }

    // -------------------------------------------------------------
    // POST: Add additional info
    // -------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("anonymous-tracking")]
    public async Task<IActionResult> AddInfo(AddAnonymousInfoDto model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Invalid data. Please check the fields and try again.";
            return RedirectToAction(nameof(View), new { code = model.ReferenceCode });
        }

        var success = await _complaintService.AddAnonymousInfoAsync(model, ct);
        TempData[success ? "Success" : "Error"] = success
            ? "Your information has been added."
            : "Failed to add information. Please try again.";

        return RedirectToAction(nameof(View), new { code = model.ReferenceCode });
    }

    private async Task<int> GetEvidenceMaxFileSizeMbAsync()
    {
        var value = await _context.SystemSettings.AsNoTracking()
            .Where(setting => setting.Key == ApplicationSettingKeys.EvidenceMaxFileSizeMb)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync();
        return int.TryParse(value, out var size) && size >= 1 && size <= 25 ? size : 10;
    }

    private void SetRecaptchaViewData() => ViewBag.RecaptchaSiteKey = _environment.IsDevelopment()
        ? _configuration["GoogleReCaptcha:Development:SiteKey"] ?? _configuration["GoogleReCaptcha:SiteKey"]
        : _configuration["GoogleReCaptcha:SiteKey"];
}
