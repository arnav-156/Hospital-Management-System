using Hospital.Application.DTOs.Catalog;
using Hospital.Application.DTOs;

namespace Hospital.Application.Interfaces;

public interface ICatalogService
{
    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(PaginationRequest pagination, CancellationToken cancellationToken);
    Task<DepartmentDto> GetDepartmentAsync(int departmentId, CancellationToken cancellationToken);
    Task<DepartmentDto> CreateDepartmentAsync(SaveDepartmentRequest request, CancellationToken cancellationToken);
    Task<DepartmentDto> UpdateDepartmentAsync(int departmentId, SaveDepartmentRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<DoctorSummaryDto>> GetDoctorsAsync(int? departmentId, PaginationRequest pagination, CancellationToken cancellationToken);
    Task<DoctorSummaryDto> GetDoctorAsync(int doctorId, CancellationToken cancellationToken);
}
