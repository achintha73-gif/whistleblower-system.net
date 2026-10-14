using Microsoft.EntityFrameworkCore;
using Whistleblowing.Application.DTOs;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Domain.Enums;
using Whistleblowing.Infrastructure.Data;

namespace Whistleblowing.Infrastructure.Services;

public class CaseService : ICaseService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public CaseService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    // -------------------------------------------------------------
    // List — role-based
    // -------------------------------------------------------------

    public async Task<IReadOnlyList<CaseListItemDto>> GetCasesForUserAsync(
        Guid userId, string role, CancellationToken ct = default)
    {
        var query = _db.Cases
            .Include(c => c.Complaint)
            .Include(c => c.AssignedInvestigator)
            .Include(c => c.Investigators)
            .AsNoTracking()
            .AsQueryable();

        if (role == RoleNames.Investigator)
            query = query.Where(c =>
                c.AssignedInvestigatorId == userId ||
                c.Investigators.Any(ci => ci.InvestigatorId == userId));

        var cases = await query
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .ToListAsync(ct);

        return cases.Select(c => new CaseListItemDto
        {
            Id = c.Id,
            ComplaintTitle = c.Complaint.Title,
            ComplaintCategory = c.Complaint.Category,
            ComplaintIsAnonymous = c.Complaint.IsAnonymous,
            Status = c.Status,
            Priority = c.Priority,
            AssignedInvestigatorName = c.AssignedInvestigator?.FullName,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
        }).ToList();
    }

    // -------------------------------------------------------------
    // Detail
    // -------------------------------------------------------------

    public async Task<CaseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _db.Cases
            .Include(x => x.Complaint)
            .Include(x => x.AssignedInvestigator)
            .Include(x => x.Investigators)
            .Include(x => x.Evidence)
            .Include(x => x.StatusHistory)
            .Include(x => x.InvestigationReport)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (c == null) return null;

        return new CaseDto
        {
            Id = c.Id,
            ComplaintId = c.ComplaintId,
            ComplaintTitle = c.Complaint.Title,
            ComplaintDescription = c.Complaint.Description,
            ComplaintCategory = c.Complaint.Category,
            ComplaintIsAnonymous = c.Complaint.IsAnonymous,
            Status = c.Status,
            Priority = c.Priority,
            ClosedAt = c.ClosedAt,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            AssignedInvestigatorId = c.AssignedInvestigatorId,
            AssignedInvestigatorName = c.AssignedInvestigator?.FullName,
            AssignedInvestigatorEmail = c.AssignedInvestigator?.Email,
            EvidenceCount = c.Evidence?.Count ?? 0,
            StatusHistoryCount = c.StatusHistory?.Count ?? 0,
            InvestigatorCount = c.Investigators?.Count ?? 0,
            HasReport = c.InvestigationReport != null,
        };
    }

    // -------------------------------------------------------------
    // Access check
    // -------------------------------------------------------------

    public async Task<bool> CanAccessAsync(
        Guid caseId, Guid userId, string role, CancellationToken ct = default)
    {
        if (role == RoleNames.Manager || role == RoleNames.Admin)
            return await _db.Cases.AnyAsync(c => c.Id == caseId, ct);

        if (role == RoleNames.Investigator)
            return await _db.Cases.AnyAsync(c =>
                c.Id == caseId &&
                (c.AssignedInvestigatorId == userId ||
                 c.Investigators.Any(ci => ci.InvestigatorId == userId)), ct);

        return false;
    }

    // -------------------------------------------------------------
    // Convert — preview
    // -------------------------------------------------------------

    public async Task<ConvertComplaintDto?> GetConversionPreviewAsync(
        Guid complaintId, CancellationToken ct = default)
    {
        var complaint = await _db.Complaints
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == complaintId, ct);

        if (complaint == null) return null;
        if (complaint.Status != ComplaintStatus.Pending) return null;

        return new ConvertComplaintDto
        {
            ComplaintId = complaint.Id,
            ComplaintTitle = complaint.Title,
            ComplaintDescription = complaint.Description,
            ComplaintCategory = complaint.Category,
            ComplaintIsAnonymous = complaint.IsAnonymous,
            Priority = CasePriority.Medium,
        };
    }

    // -------------------------------------------------------------
    // Convert — execute
    // -------------------------------------------------------------

    public async Task<ConvertComplaintResult> ConvertFromComplaintAsync(
        Guid complaintId, CasePriority priority, Guid currentUserId,
        CancellationToken ct = default)
    {
        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var complaint = await _db.Complaints
                .FirstOrDefaultAsync(c => c.Id == complaintId, ct);

            if (complaint == null)
                return new ConvertComplaintResult(false, Errors: new[] { "Complaint not found" });

            if (complaint.Status != ComplaintStatus.Pending)
                return new ConvertComplaintResult(false, Errors: new[] { "Only pending complaints can be converted" });

            var existing = await _db.Cases.AnyAsync(c => c.ComplaintId == complaintId, ct);
            if (existing)
                return new ConvertComplaintResult(false, Errors: new[] { "A case already exists for this complaint" });

            var newCase = new Case
            {
                ComplaintId = complaintId,
                Status = CaseStatus.Open,
                Priority = priority,
            };

            _db.Cases.Add(newCase);
            await _db.SaveChangesAsync(ct);

            complaint.Status = ComplaintStatus.ConvertedToCase;
            await _db.SaveChangesAsync(ct);

            _db.CaseStatusHistories.Add(new CaseStatusHistory
            {
                CaseId = newCase.Id,
                Status = CaseStatus.Open,
                Note = "Case created from complaint",
                ChangedById = currentUserId,
                ChangedAt = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
            return new ConvertComplaintResult(true, CaseId: newCase.Id);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return new ConvertComplaintResult(false, Errors: new[] { ex.Message });
        }
    }

    // -------------------------------------------------------------
    // Assign — preview
    // -------------------------------------------------------------

    public async Task<AssignInvestigatorDto?> GetAssignPreviewAsync(
        Guid caseId, CancellationToken ct = default)
    {
        var caseRow = await _db.Cases
            .Include(c => c.Complaint)
            .Include(c => c.Investigators)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId, ct);

        if (caseRow == null) return null;

        var selected = caseRow.Investigators.Select(i => i.InvestigatorId).ToList();

        // Get Investigator role ID
        var invRoleId = await _db.Roles
            .Where(r => r.Name == RoleNames.Investigator)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        // Get all active investigators via UserRoles join
        var investigators = await _db.Users
            .Include(u => u.Department)
            .Where(u => u.Status == UserStatus.Active &&
                        _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == invRoleId))
            .OrderBy(u => u.FullName)
            .Select(u => new InvestigatorOptionDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                DepartmentName = u.Department != null ? u.Department.Name : null,
            })
            .ToListAsync(ct);

        return new AssignInvestigatorDto
        {
            CaseId = caseRow.Id,
            CaseTitle = caseRow.Complaint.Title,
            SelectedInvestigatorIds = selected,
            AvailableInvestigators = investigators,
        };
    }

    // -------------------------------------------------------------
    // Assign — execute
    // -------------------------------------------------------------

    public async Task<AssignResult> AssignInvestigatorsAsync(
        Guid caseId, List<Guid> investigatorIds, Guid currentUserId,
        CancellationToken ct = default)
    {
        var uniqueIds = investigatorIds.Distinct().ToList();

        // Validate investigators
        if (uniqueIds.Count > 0)
        {
            var invRoleId = await _db.Roles
                .Where(r => r.Name == RoleNames.Investigator)
                .Select(r => r.Id)
                .FirstOrDefaultAsync(ct);

            var validCount = await _db.Users
                .CountAsync(u => uniqueIds.Contains(u.Id) &&
                                 u.Status == UserStatus.Active &&
                                 _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == invRoleId), ct);

            if (validCount != uniqueIds.Count)
                return new AssignResult(false, Errors: new[] { "One or more investigators are invalid or inactive" });
        }

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var caseRow = await _db.Cases
                .Include(c => c.Complaint)
                .Include(c => c.Investigators)
                .FirstOrDefaultAsync(c => c.Id == caseId, ct);

            if (caseRow == null)
                return new AssignResult(false, Errors: new[] { "Case not found" });

            var beforeIds = caseRow.Investigators.Select(i => i.InvestigatorId).ToList();

            // Remove all current
            _db.CaseInvestigators.RemoveRange(caseRow.Investigators);

            // Add new
            foreach (var id in uniqueIds)
            {
                _db.CaseInvestigators.Add(new CaseInvestigator
                {
                    CaseId = caseId,
                    InvestigatorId = id,
                    AssignedAt = DateTime.UtcNow,
                });
            }

            // Primary = first
            caseRow.AssignedInvestigatorId = uniqueIds.Count > 0 ? uniqueIds[0] : null;

            // Auto advance Open -> Investigating
            var shouldAdvance = caseRow.Status == CaseStatus.Open && uniqueIds.Count > 0;
            if (shouldAdvance)
            {
                caseRow.Status = CaseStatus.Investigating;
                _db.CaseStatusHistories.Add(new CaseStatusHistory
                {
                    CaseId = caseId,
                    Status = CaseStatus.Investigating,
                    Note = "Case moved to Investigating after investigator assignment",
                    ChangedById = currentUserId,
                    ChangedAt = DateTime.UtcNow,
                });
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // Diff
            var added = uniqueIds.Except(beforeIds).ToList();
            var removed = beforeIds.Except(uniqueIds).ToList();

            // Notify added
            foreach (var id in added)
                await _notifications.NotifyAsync(id,
                    $"You have been assigned to Case: {caseRow.Complaint.Title}", ct);

            // Notify removed
            foreach (var id in removed)
                await _notifications.NotifyAsync(id,
                    $"You have been removed from Case: {caseRow.Complaint.Title}", ct);

            return new AssignResult(true, added, removed);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return new AssignResult(false, Errors: new[] { ex.Message });
        }
    }
}