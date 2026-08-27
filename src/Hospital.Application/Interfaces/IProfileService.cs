using Hospital.Application.DTOs.Profiles;
using Hospital.Application.DTOs;

namespace Hospital.Application.Interfaces;

public interface IProfileService
{
    Task<object> GetCurrentProfileAsync(int userId, string role, CancellationToken cancellationToken);
    Task<PatientProfileDto> UpdatePatientProfileAsync(int userId, UpdatePatientProfileRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PatientProfileDto>> GetPatientsAsync(string? search, PaginationRequest pagination, CancellationToken cancellationToken);
    Task<IReadOnlyList<DoctorProfileDto>> GetDoctorsAsync(string? search, PaginationRequest pagination, CancellationToken cancellationToken);
    Task<IReadOnlyList<StaffProfileDto>> GetStaffAsync(string? search, PaginationRequest pagination, CancellationToken cancellationToken);
    Task<UserAccountDto> UpdateAccountStatusAsync(int userId, bool isActive, CancellationToken cancellationToken);
}
