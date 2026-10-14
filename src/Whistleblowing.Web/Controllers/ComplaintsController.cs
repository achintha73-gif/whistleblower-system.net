using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Whistleblowing.Application.DTOs;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Entities;

namespace Whistleblowing.Web.Controllers;

[Authorize]
public class ComplaintsController : Controller
{
    private readonly IComplaintService _complaints;
    private readonly UserManager<ApplicationUser> _userManager;

    public ComplaintsController(IComplaintService complaints, UserManager<ApplicationUser> userManager)
    {
        _complaints = complaints;
        _userManager = userManager;
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
    public IActionResult Create()
    {
        ViewBag.Categories = ComplaintCategories.All;
        return View(new CreateComplaintDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> Create(CreateComplaintDto model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = ComplaintCategories.All;
            return View(model);
        }

        var created = await _complaints.CreateAsync(user.Id, model);
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
        ViewBag.CanConvert = (isManager || isAdmin) && complaint.Status == Domain.Enums.ComplaintStatus.Pending;
        ViewBag.CanEdit = (isEmployee && complaint.UserId == user.Id) || isAdmin;

        return View(complaint);
    }
}