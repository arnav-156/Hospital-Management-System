namespace Hospital.Application.DTOs.Billing;

public sealed record BillDto(int BillId, int AppointmentId, int PatientId, int? GeneratedByDoctorId, decimal Amount, string Status, string? Description, DateTime GeneratedAt, DateOnly? DueDate, DateTime? PaidAt, DateTime? VoidedAt, string? VoidReason);
