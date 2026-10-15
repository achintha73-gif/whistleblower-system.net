using Microsoft.EntityFrameworkCore;
using Whistleblowing.Application.DTOs;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Domain.Enums;
using Whistleblowing.Infrastructure.Data;

namespace Whistleblowing.Infrastructure.Services;

public class ComplaintService : IComplaintService
{
    private readonly AppDbContext _db;
    private readonly IReferenceCodeService _codeService;

    public ComplaintService(AppDbContext db, IReferenceCodeService codeService)
    {
        _db = db;
        _codeService = codeService;
    }

    // ============================================================
    // EXISTING METHODS
    // ============================================================

    public async Task<IReadOnlyList<ComplaintDto>> GetMyComplaintsAsync(Guid userId, CancellationToken ct = default)
    {
        var complaints = await _db.Complaints
            .Include(c => c.User)
            .Include(c => c.Case)
            .Include(c => c.AdditionalInfo)
            .Include(c => c.Evidence).ThenInclude(e => e.UploadedBy)
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.IsAnonymous)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return complaints.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ComplaintDto>> GetAllComplaintsAsync(CancellationToken ct = default)
    {
        var complaints = await _db.Complaints
            .Include(c => c.User)
            .Include(c => c.Case)
            .Include(c => c.AdditionalInfo)
            .Include(c => c.Evidence).ThenInclude(e => e.UploadedBy)
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return complaints.Select(MapToDto).ToList();
    }

    public async Task<ComplaintDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var complaint = await _db.Complaints
            .Include(c => c.User)
            .Include(c => c.Case)
            .Include(c => c.AdditionalInfo)
            .Include(c => c.Evidence).ThenInclude(e => e.UploadedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return complaint == null ? null : MapToDto(complaint);
    }

    public async Task<ComplaintDto> CreateAsync(Guid userId, CreateComplaintDto input, CancellationToken ct = default)
    {
        var isAnonymous = input.IsAnonymous;
        var complaint = new Complaint
        {
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            Category = string.IsNullOrWhiteSpace(input.Category) ? null : input.Category.Trim(),
            Status = ComplaintStatus.Pending,
            IsAnonymous = isAnonymous,
            UserId = isAnonymous ? null : userId,
            ReferenceCode = isAnonymous ? await _codeService.GenerateUniqueAsync(ct) : null,
        };

        _db.Complaints.Add(complaint);
        _db.ActivityLogs.Add(new ActivityLog
        {
            EventType = "ComplaintSubmitted",
            Description = input.IsAnonymous ? "An anonymous concern was submitted." : "A concern was submitted.",
            EntityType = "Complaint",
            EntityId = complaint.Id,
            ActorId = isAnonymous ? null : userId,
        });
        await _db.SaveChangesAsync(ct);

        var created = await _db.Complaints
            .Include(c => c.User)
            .AsNoTracking()
            .FirstAsync(c => c.Id == complaint.Id, ct);

        return MapToDto(created);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var complaint = await _db.Complaints
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

        if (complaint == null) return false;
        if (complaint.Status != ComplaintStatus.Pending) return false;

        _db.Complaints.Remove(complaint);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ============================================================
    // ANONYMOUS METHODS
    // ============================================================

    public async Task<AnonymousComplaintResultDto> SubmitAnonymousAsync(
        SubmitAnonymousComplaintDto input,
        CancellationToken ct = default)
    {
        var code = await _codeService.GenerateUniqueAsync(ct);

        var complaint = new Complaint
        {
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            Category = string.IsNullOrWhiteSpace(input.Category) ? null : input.Category.Trim(),
            Status = ComplaintStatus.Pending,
            IsAnonymous = true,
            UserId = null,
            ReferenceCode = code,
        };

        _db.Complaints.Add(complaint);
        _db.ActivityLogs.Add(new ActivityLog
        {
            EventType = "ComplaintSubmitted",
            Description = "An anonymous concern was submitted.",
            EntityType = "Complaint",
            EntityId = complaint.Id,
        });
        await _db.SaveChangesAsync(ct);

        return MapToAnonymousDto(complaint);
    }

    public async Task<AnonymousComplaintResultDto?> TrackByReferenceCodeAsync(
        string referenceCode,
        CancellationToken ct = default)
    {
        var normalized = referenceCode.Trim().ToUpperInvariant();

        var complaint = await _db.Complaints
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ReferenceCode == normalized && c.IsAnonymous, ct);

        return complaint == null ? null : MapToAnonymousDto(complaint);
    }

    public async Task<bool> AddAnonymousInfoAsync(
        AddAnonymousInfoDto input,
        CancellationToken ct = default)
    {
        var normalized = input.ReferenceCode.Trim().ToUpperInvariant();

        var complaint = await _db.Complaints
            .FirstOrDefaultAsync(c => c.ReferenceCode == normalized && c.IsAnonymous, ct);

        if (complaint == null) return false;

        var info = new AdditionalInformation
        {
            ComplaintId = complaint.Id,
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            InformationType = input.InformationType,

        };

        _db.AdditionalInformation.Add(info);
        _db.ActivityLogs.Add(new ActivityLog
        {
            EventType = "AnonymousAdditionalInformationSubmitted",
            Description = "Additional information was submitted for an anonymous concern.",
            EntityType = "Complaint",
            EntityId = complaint.Id,
        });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<AnonymousInfoDto>> GetAnonymousInfoAsync(
        string referenceCode,
        CancellationToken ct = default)
    {
        var normalized = referenceCode.Trim().ToUpperInvariant();

        var complaint = await _db.Complaints
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ReferenceCode == normalized && c.IsAnonymous, ct);

        if (complaint == null) return Array.Empty<AnonymousInfoDto>();

        var infos = await _db.AdditionalInformation
            .AsNoTracking()
            .Where(i => i.ComplaintId == complaint.Id)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        return infos.Select(i => new AnonymousInfoDto
        {
            Id = i.Id,
            Title = i.Title,
            Description = i.Description,
            InformationType = i.InformationType.ToString(),
            SubmittedAt = i.CreatedAt,
        }).ToList();
    }

    // ============================================================
    // MAPPERS
    // ============================================================

    private static ComplaintDto MapToDto(Complaint c)
    {
        return new ComplaintDto
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            Category = c.Category,
            Status = c.Status,
            IsAnonymous = c.IsAnonymous,
            ReferenceCode = c.ReferenceCode,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            UserId = c.UserId,
            UserName = c.User?.FullName,
            UserEmail = c.User?.Email,
            CaseId = c.Case?.Id,
            AdditionalInfoCount = c.AdditionalInfo?.Count ?? 0,
            EvidenceCount = c.Evidence?.Count ?? 0,
            Evidence = c.Evidence?.OrderByDescending(e => e.UploadedAt).Select(e => new EvidenceItemDto
            {
                Id = e.Id,
                FileName = e.FileName,
                Description = e.Description,
                UploadedAt = e.UploadedAt,
                UploadedBy = e.UploadedBy?.FullName,
            }).ToList() ?? new List<EvidenceItemDto>(),
            AdditionalInformation = c.AdditionalInfo?.OrderByDescending(i => i.CreatedAt).Select(i => new AdditionalInformationItemDto
            {
                Id = i.Id,
                Title = i.Title,
                Description = i.Description,
                InformationType = i.InformationType.ToString(),
                SubmittedAt = i.CreatedAt,
            }).ToList() ?? new List<AdditionalInformationItemDto>(),
        };
    }

    private static AnonymousComplaintResultDto MapToAnonymousDto(Complaint c)
    {
        return new AnonymousComplaintResultDto
        {
            Id = c.Id,
            ReferenceCode = c.ReferenceCode ?? string.Empty,
            Title = c.Title,
            Description = c.Description,
            Category = c.Category,
            Status = c.Status,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
        };
    }
}


