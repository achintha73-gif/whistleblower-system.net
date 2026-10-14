using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Whistleblowing.Application.DTOs;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Web.Controllers;

[Authorize(Roles = "Manager,Investigator,Admin")]
public class CasesController : Controller
{
    private readonly ICaseService _cases;
    private readonly UserManager<ApplicationUser> _userManager;

    public CasesController(ICaseService cases, UserManager<ApplicationUser> userManager)
    {
        _cases = cases;
        _userManager = userManager;
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

        return View(caseDetail);
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

        TempData["Success"] = "Investigators updated successfully";
        return RedirectToAction(nameof(Detail), new { id = caseId });
    }
}