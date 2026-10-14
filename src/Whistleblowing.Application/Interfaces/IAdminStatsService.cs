namespace Whistleblowing.Application.Interfaces;

public interface IAdminStatsService
{
    Task<AdminStatsDto> GetStatsAsync(CancellationToken ct = default);
}

public record AdminStatsDto(
    int TotalUsers,
    int ActiveUsers,
    int SuspendedUsers,
    int TotalDepartments
);