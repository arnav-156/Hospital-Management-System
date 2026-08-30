namespace Hospital.Application.DTOs.Profiles;

public sealed record DoctorProfileDto(int UserId, int DoctorId, string Email, string FirstName, string LastName, string LicenseNumber, string Specialization, int DepartmentId, string DepartmentName, string? PhoneNumber, decimal ConsultationFee, bool IsActive, bool IsAccountActive);
