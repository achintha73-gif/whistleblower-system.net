using Microsoft.EntityFrameworkCore;
using Whistleblowing.Application.DTOs;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Infrastructure.Data;

namespace Whistleblowing.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task NotifyAsync(Guid userId, string message, CancellationToken ct = default)
    {
        var n = new Notification
        {
            UserId = userId,
            Message = message,
            IsRead = false,
        };
        _db.Notifications.Add(n);
        await _db.SaveChangesAsync(ct);
    }

    public async Task NotifyManyAsync(IEnumerable<Guid> userIds, string message, CancellationToken ct = default)
    {
        var list = userIds.Distinct().ToList();
        if (list.Count == 0) return;

        foreach (var uid in list)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = uid,
                Message = message,
                IsRead = false,
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
            })
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var n = await _db.Notifications
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, ct);
        if (n == null) return false;

        n.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}