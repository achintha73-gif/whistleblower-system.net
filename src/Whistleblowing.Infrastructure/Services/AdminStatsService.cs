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
        var totalUsers = await _db.Users.CountAsync(ct);

        var activeUsers = await _db.Users
            .CountAsync(u => u.Status == UserStatus.Active, ct);

        var suspendedUsers = await _db.Users
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