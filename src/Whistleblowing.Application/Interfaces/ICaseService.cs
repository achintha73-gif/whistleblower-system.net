using Whistleblowing.Application.DTOs;
using Whistleblowing.Domain.Enums;

namespace Whistleblowing.Application.Interfaces;

public interface ICaseService
{
    Task<IReadOnlyList<CaseListItemDto>> GetCasesForUserAsync(
        Guid userId, string role, CancellationToken ct = default);

    Task<CaseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<bool> CanAccessAsync(
        Guid caseId, Guid userId, string role, CancellationToken ct = default);

    Task<ConvertComplaintResult> ConvertFromComplaintAsync(
        Guid complaintId, CasePriority priority, Guid currentUserId,
        CancellationToken ct = default);

    Task<ConvertComplaintDto?> GetConversionPreviewAsync(
        Guid complaintId, CancellationToken ct = default);

    // -----------------------------
    // Investigator assignment
    // -----------------------------

    Task<AssignInvestigatorDto?> GetAssignPreviewAsync(
        Guid caseId, CancellationToken ct = default);

    Task<AssignResult> AssignInvestigatorsAsync(
        Guid caseId, List<Guid> investigatorIds, Guid currentUserId,
        CancellationToken ct = default);
}