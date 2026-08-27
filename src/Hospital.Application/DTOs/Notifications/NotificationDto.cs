namespace Hospital.Application.DTOs.Notifications;
public sealed record NotificationDto(int NotificationId, string NotificationType, string Message, bool IsRead, DateTime CreatedAt, DateTime? ReadAt);
