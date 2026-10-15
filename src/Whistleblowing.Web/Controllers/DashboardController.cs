using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Application.DTOs;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAdminStatsService _adminStats;
    private readonly IComplaintService _complaints;
    private readonly ICaseService _cases;
    private readonly INotificationService _notifications;

    public DashboardController(
        UserManager<ApplicationUser> userManager,
        IAdminStatsService adminStats,
        IComplaintService complaints,
        ICaseService cases,
        INotificationService notifications)
    {
        _userManager = userManager;
        _adminStats = adminStats;
        _complaints = complaints;
        _cases = cases;
        _notifications = notifications;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains(RoleNames.Admin)) return RedirectToAction(nameof(Admin));
        if (roles.Contains(RoleNames.Manager)) return RedirectToAction(nameof(Manager));
        if (roles.Contains(RoleNames.Investigator)) return RedirectToAction(nameof(Investigator));

        return RedirectToAction(nameof(Employee));
    }

    public async Task<IActionResult> Admin()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !await _userManager.IsInRoleAsync(user, RoleNames.Admin))
            return RedirectToAction("AccessDenied", "Account");

        ViewBag.FullName = user.FullName;
        ViewBag.Stats = await _adminStats.GetStatsAsync();
        return View();
    }

    public async Task<IActionResult> Manager()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !await _userManager.IsInRoleAsync(user, RoleNames.Manager))
            return RedirectToAction("AccessDenied", "Account");

        ViewBag.FullName = user.FullName;
        var complaints = await _complaints.GetAllComplaintsAsync();
        var cases = await _cases.GetCasesForUserAsync(user.Id, RoleNames.Manager);
        var activeCases = cases.Where(c => c.Status is not CaseStatus.Closed and not CaseStatus.Archived).ToList();
        ViewBag.PendingConcerns = complaints.Count(c => c.Status == ComplaintStatus.Pending);
        ViewBag.TotalConcerns = complaints.Count;
        ViewBag.UnderReviewConcerns = complaints.Count(c => c.Status == ComplaintStatus.UnderReview);
        ViewBag.ActiveCases = activeCases.Count;
        ViewBag.OpenCases = cases.Count(c => c.Status == CaseStatus.Open);
        ViewBag.PendingReviewCases = cases.Count(c => c.Status == CaseStatus.PendingReview);
        ViewBag.UnassignedCases = activeCases.Count(c => c.AssignedInvestigatorName == null);
        ViewBag.CriticalCases = activeCases.Count(c => c.Priority == CasePriority.Critical);
        ViewBag.ActiveCaseAssignments = activeCases;
        ViewBag.RecentConcerns = complaints.Take(5).ToList();
        ViewBag.RecentCases = cases.Take(5).ToList();
        var notifications = await _notifications.GetForUserAsync(user.Id);
        ViewBag.UnreadNotificationCount = notifications.Count(n => !n.IsRead);
        ViewBag.RecentNotifications = notifications.Take(3).ToList();
        return View(complaints.Take(5).ToList());
    }

    public async Task<IActionResult> Investigator()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !await _userManager.IsInRoleAsync(user, RoleNames.Investigator))
            return RedirectToAction("AccessDenied", "Account");

        ViewBag.FullName = user.FullName;
        var assignedCases = await _cases.GetCasesForUserAsync(user.Id, RoleNames.Investigator);
        ViewBag.ActiveCases = assignedCases.Count(c => c.Status != CaseStatus.Closed && c.Status != CaseStatus.Archived);
        ViewBag.OpenCases = assignedCases.Count(c => c.Status == CaseStatus.Open);
        ViewBag.InvestigatingCases = assignedCases.Count(c => c.Status == CaseStatus.Investigating);
        ViewBag.PendingReviewCases = assignedCases.Count(c => c.Status == CaseStatus.PendingReview);
        ViewBag.HighPriorityCases = assignedCases.Count(c => c.Status != CaseStatus.Closed && c.Status != CaseStatus.Archived && c.Priority is CasePriority.High or CasePriority.Critical);
        ViewBag.ReportsToComplete = assignedCases.Count(c => c.Status == CaseStatus.Investigating && !c.HasReport);
        var notifications = await _notifications.GetForUserAsync(user.Id);
        ViewBag.UnreadNotificationCount = notifications.Count(n => !n.IsRead);
        ViewBag.RecentNotifications = notifications.Take(3).ToList();
        return View(assignedCases.Take(8).ToList());
    }

    public async Task<IActionResult> Employee()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        ViewBag.FullName = user.FullName;
        var complaints = await _complaints.GetMyComplaintsAsync(user.Id);
        ViewBag.TotalConcerns = complaints.Count;
        ViewBag.PendingCount = complaints.Count(c => c.Status == ComplaintStatus.Pending);
        ViewBag.UnderReviewCount = complaints.Count(c => c.Status == ComplaintStatus.UnderReview);
        ViewBag.ApprovedCount = complaints.Count(c => c.Status == ComplaintStatus.Approved);
        ViewBag.RejectedCount = complaints.Count(c => c.Status == ComplaintStatus.Rejected);
        ViewBag.DecidedCount = complaints.Count(c => c.Status is ComplaintStatus.Approved or ComplaintStatus.Rejected);
        ViewBag.ConvertedCount = complaints.Count(c => c.Status == ComplaintStatus.ConvertedToCase);
        var notifications = await _notifications.GetForUserAsync(user.Id);
        ViewBag.UnreadNotificationCount = notifications.Count(n => !n.IsRead);
        ViewBag.RecentNotifications = notifications.Take(3).ToList();
        return View(complaints.Take(5).ToList());
    }
}
