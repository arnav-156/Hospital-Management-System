namespace Hospital.Application.DTOs.Profiles;

public sealed record PatientProfileDto(int UserId, int PatientId, string Email, string MedicalRecordNumber, string FirstName, string LastName, DateOnly DateOfBirth, string? Gender, string? PhoneNumber, string? Address, string? EmergencyContactName, string? EmergencyContactPhone, bool IsAccountActive);
