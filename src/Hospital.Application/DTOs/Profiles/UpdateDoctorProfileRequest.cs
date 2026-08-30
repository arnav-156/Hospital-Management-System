using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Profiles;

public sealed class UpdateDoctorProfileRequest
{
    [Required, StringLength(100)] public string FirstName { get; init; } = string.Empty;
    [Required, StringLength(100)] public string LastName { get; init; } = string.Empty;
    [Required, StringLength(100)] public string LicenseNumber { get; init; } = string.Empty;
    [Required, StringLength(150)] public string Specialization { get; init; } = string.Empty;
    [Range(1, int.MaxValue)] public int DepartmentId { get; init; }
    [StringLength(30)] public string? PhoneNumber { get; init; }
    [Range(typeof(decimal), "0", "1000000")] public decimal ConsultationFee { get; init; }
    public bool IsActive { get; init; }
}
