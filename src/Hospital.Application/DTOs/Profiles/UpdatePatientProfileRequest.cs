using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Profiles;

public sealed class UpdatePatientProfileRequest
{
    [Required, StringLength(100)] public string FirstName { get; init; } = string.Empty;
    [Required, StringLength(100)] public string LastName { get; init; } = string.Empty;
    [Required] public DateOnly? DateOfBirth { get; init; }
    [RegularExpression("^(Female|Male|NonBinary|Undisclosed)$")] public string? Gender { get; init; }
    [StringLength(30)] public string? PhoneNumber { get; init; }
    [StringLength(500)] public string? Address { get; init; }
    [StringLength(200)] public string? EmergencyContactName { get; init; }
    [StringLength(30)] public string? EmergencyContactPhone { get; init; }
}
