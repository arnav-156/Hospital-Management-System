using Hospital.Application.DTOs.Notifications;
using Hospital.Application.DTOs;

namespace Hospital.Application.Interfaces;

public interface INotificationService { Task CreateAsync(int userId, string type, string message, CancellationToken cancellationToken); Task<IReadOnlyList<NotificationDto>> GetAsync(int userId, PaginationRequest pagination, CancellationToken cancellationToken); Task<NotificationDto> MarkReadAsync(int userId, int notificationId, CancellationToken cancellationToken); }
