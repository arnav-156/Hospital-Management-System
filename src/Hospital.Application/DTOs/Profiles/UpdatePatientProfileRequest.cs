using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Profiles;

public sealed class UpdatePatientProfileRequest
{
    [Required, StringLength(100)] public string FirstName { get; init; } = string.Empty;
    [Required, StringLength(100)] public string LastName { get; init; } = string.Empty;
    [Required] public DateOnly? DateOfBirth { get; init; }
    [StringLength(20)] public string? Gender { get; init; }
    [StringLength(30)] public string? PhoneNumber { get; init; }
    [StringLength(500)] public string? Address { get; init; }
    [StringLength(200)] public string? EmergencyContactName { get; init; }
    [StringLength(30)] public string? EmergencyContactPhone { get; init; }
}
