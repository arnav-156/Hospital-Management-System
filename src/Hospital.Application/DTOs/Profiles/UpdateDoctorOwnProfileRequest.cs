using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Profiles;

public sealed class UpdateDoctorOwnProfileRequest
{
    [Required, StringLength(100)] public string FirstName { get; init; } = string.Empty;
    [Required, StringLength(100)] public string LastName { get; init; } = string.Empty;
    [Required, StringLength(150)] public string Specialization { get; init; } = string.Empty;
    [StringLength(30)] public string? PhoneNumber { get; init; }
    [Range(typeof(decimal), "0", "1000000")] public decimal ConsultationFee { get; init; }
}
