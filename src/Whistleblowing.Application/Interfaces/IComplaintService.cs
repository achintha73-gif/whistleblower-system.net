using Whistleblowing.Application.DTOs;

namespace Whistleblowing.Application.Interfaces;

public interface IComplaintService
{
    // -------- Existing (authenticated) methods --------
    Task<IReadOnlyList<ComplaintDto>> GetMyComplaintsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<ComplaintDto>> GetAllComplaintsAsync(CancellationToken ct = default);
    Task<ComplaintDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ComplaintDto> CreateAsync(Guid userId, CreateComplaintDto input, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);

    // -------- Anonymous methods (no authentication) --------
    Task<AnonymousComplaintResultDto> SubmitAnonymousAsync(
        SubmitAnonymousComplaintDto input,
        CancellationToken ct = default);

    Task<AnonymousComplaintResultDto?> TrackByReferenceCodeAsync(
        string referenceCode,
        CancellationToken ct = default);

    Task<bool> AddAnonymousInfoAsync(
        AddAnonymousInfoDto input,
        CancellationToken ct = default);

    Task<IReadOnlyList<AnonymousInfoDto>> GetAnonymousInfoAsync(
        string referenceCode,
        CancellationToken ct = default);
}

public class AnonymousInfoDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InformationType { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}
