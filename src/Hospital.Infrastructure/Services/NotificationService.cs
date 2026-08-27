using Hospital.Application.DTOs.Notifications;
using Hospital.Application.DTOs;
using Hospital.Application.Exceptions;
using Hospital.Application.Interfaces;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public sealed class NotificationService(HospitalManagementDbContext dbContext, TimeProvider timeProvider) : INotificationService
{
    public async Task CreateAsync(int userId, string type, string message, CancellationToken cancellationToken)
    {
        dbContext.Notifications.Add(new Notification { UserId = userId, NotificationType = type, Message = message, CreatedAt = timeProvider.GetUtcNow().UtcDateTime, IsRead = false });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetAsync(int userId, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var notifications = await dbContext.Notifications.AsNoTracking().Where(notification => notification.UserId == userId).OrderByDescending(notification => notification.CreatedAt).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        return notifications.Select(ToDto).ToList();
    }

    public async Task<NotificationDto> MarkReadAsync(int userId, int notificationId, CancellationToken cancellationToken)
    {
        var notification = await dbContext.Notifications.SingleOrDefaultAsync(candidate => candidate.NotificationId == notificationId && candidate.UserId == userId, cancellationToken) ?? throw new NotFoundException("Notification not found.");
        if (!notification.IsRead) { notification.IsRead = true; notification.ReadAt = timeProvider.GetUtcNow().UtcDateTime; await dbContext.SaveChangesAsync(cancellationToken); }
        return ToDto(notification);
    }

    private static NotificationDto ToDto(Notification notification) => new(notification.NotificationId, notification.NotificationType, notification.Message, notification.IsRead, notification.CreatedAt, notification.ReadAt);
}
