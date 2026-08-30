using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Profiles;

public sealed class UpdateStaffProfileRequest
{
    [Required, StringLength(100)] public string FirstName { get; init; } = string.Empty;
    [Required, StringLength(100)] public string LastName { get; init; } = string.Empty;
    [Required, StringLength(50)] public string EmployeeNumber { get; init; } = string.Empty;
    [Required, StringLength(150)] public string JobTitle { get; init; } = string.Empty;
    [Range(1, int.MaxValue)] public int? DepartmentId { get; init; }
    [StringLength(30)] public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; }
}
