namespace Hospital.Application.DTOs.Billing;

public sealed record PaymentDto(int PaymentId, int BillId, decimal Amount, string PaymentMethod, string? ReferenceNumber, DateTime RecordedAt);
