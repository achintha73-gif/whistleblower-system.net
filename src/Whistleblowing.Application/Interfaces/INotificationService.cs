using Whistleblowing.Application.DTOs;

namespace Whistleblowing.Application.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(Guid userId, string message, CancellationToken ct = default);

    Task NotifyManyAsync(IEnumerable<Guid> userIds, string message, CancellationToken ct = default);

    Task<IReadOnlyList<NotificationDto>> GetForUserAsync(Guid userId, CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);

    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
}