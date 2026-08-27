namespace Hospital.Application.DTOs.Profiles;

public sealed record StaffProfileDto(int UserId, int StaffId, string Email, string FirstName, string LastName, string EmployeeNumber, string JobTitle, string? DepartmentName, string? PhoneNumber, bool IsActive);
