using Microsoft.EntityFrameworkCore;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Infrastructure.Data;

namespace Whistleblowing.Infrastructure.Services;

public class AdminStatsService : IAdminStatsService
{
    private readonly AppDbContext _db;

    public AdminStatsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var activeAccounts = _db.Users.Where(u => !u.IsDeleted);
        var totalUsers = await activeAccounts.CountAsync(ct);

        var activeUsers = await activeAccounts
            .CountAsync(u => u.Status == UserStatus.Active, ct);

        var suspendedUsers = await activeAccounts
            .CountAsync(u => u.Status == UserStatus.Suspended
                          || u.Status == UserStatus.Inactive, ct);

        var totalDepartments = await _db.Departments.CountAsync(ct);

        return new AdminStatsDto(
            totalUsers,
            activeUsers,
            suspendedUsers,
            totalDepartments
        );
    }
}
