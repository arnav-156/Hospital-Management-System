namespace Hospital.Application.DTOs.Catalog;

public sealed record DoctorSummaryDto(int DoctorId, int DepartmentId, string FirstName, string LastName, string Specialization, string? PhoneNumber, decimal ConsultationFee);
