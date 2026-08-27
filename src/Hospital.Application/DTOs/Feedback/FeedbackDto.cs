namespace Hospital.Application.DTOs.Feedback;
public sealed record FeedbackDto(int FeedbackId, int AppointmentId, int PatientId, byte Rating, string? Comments, DateTime CreatedAt);
