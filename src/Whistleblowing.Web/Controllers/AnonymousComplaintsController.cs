using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Whistleblowing.Application.DTOs;
using Whistleblowing.Application.Interfaces;

namespace Whistleblowing.Web.Controllers;

[AllowAnonymous]
public class AnonymousComplaintsController : Controller
{
    private readonly IComplaintService _complaintService;

    public AnonymousComplaintsController(IComplaintService complaintService)
    {
        _complaintService = complaintService;
    }

    // -------------------------------------------------------------
    // GET: Submit form
    // -------------------------------------------------------------
    [HttpGet]
    public IActionResult Submit()
    {
        return View(new SubmitAnonymousComplaintDto());
    }

    // -------------------------------------------------------------
    // POST: Submit complaint
    // -------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitAnonymousComplaintDto model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await _complaintService.SubmitAnonymousAsync(model, ct);
            return RedirectToAction(nameof(Success), new { code = result.ReferenceCode });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Failed to submit complaint. Please try again.");
            // Log ex here in production
            return View(model);
        }
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
    public async Task<IActionResult> View(string code, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(code))
            return RedirectToAction(nameof(Track));

        var complaint = await _complaintService.TrackByReferenceCodeAsync(code, ct);
        if (complaint == null)
            return RedirectToAction(nameof(Track));

        var infos = await _complaintService.GetAnonymousInfoAsync(code, ct);
        ViewData["AdditionalInfo"] = infos;

        return View(complaint);
    }

    // -------------------------------------------------------------
    // POST: Add additional info
    // -------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
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
}
