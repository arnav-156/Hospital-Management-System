using Hospital.Application.DTOs.Catalog;
using Hospital.Application.DTOs;
using Hospital.Application.Exceptions;
using Hospital.Application.Interfaces;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public sealed class CatalogService(HospitalManagementDbContext dbContext, TimeProvider timeProvider) : ICatalogService
{
    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(PaginationRequest pagination, CancellationToken cancellationToken) =>
        await dbContext.Departments.AsNoTracking().Where(department => department.IsActive).OrderBy(department => department.Name).Skip(pagination.Skip).Take(pagination.PageSize).Select(department => ToDto(department)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DepartmentDto>> GetAllDepartmentsAsync(PaginationRequest pagination, CancellationToken cancellationToken) =>
        await dbContext.Departments.AsNoTracking().OrderBy(department => department.Name).Skip(pagination.Skip).Take(pagination.PageSize).Select(department => ToDto(department)).ToListAsync(cancellationToken);

    public async Task<DepartmentDto> GetDepartmentAsync(int departmentId, CancellationToken cancellationToken) =>
        await dbContext.Departments.AsNoTracking().Where(department => department.DepartmentId == departmentId && department.IsActive).Select(department => ToDto(department)).SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Department not found.");

    public async Task<DepartmentDto> CreateDepartmentAsync(SaveDepartmentRequest request, CancellationToken cancellationToken)
    {
        var department = new Department { DepartmentCode = request.DepartmentCode.Trim().ToUpperInvariant(), Name = request.Name.Trim(), Description = request.Description?.Trim(), IsActive = request.IsActive, CreatedAt = timeProvider.GetUtcNow().UtcDateTime };
        dbContext.Departments.Add(department);
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new ConflictException("A department with this code or name already exists."); }
        return ToDto(department);
    }

    public async Task<DepartmentDto> UpdateDepartmentAsync(int departmentId, SaveDepartmentRequest request, CancellationToken cancellationToken)
    {
        var department = await dbContext.Departments.SingleOrDefaultAsync(candidate => candidate.DepartmentId == departmentId, cancellationToken) ?? throw new NotFoundException("Department not found.");
        department.DepartmentCode = request.DepartmentCode.Trim().ToUpperInvariant(); department.Name = request.Name.Trim(); department.Description = request.Description?.Trim(); department.IsActive = request.IsActive;
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new ConflictException("A department with this code or name already exists."); }
        return ToDto(department);
    }

    public async Task<IReadOnlyList<DoctorSummaryDto>> GetDoctorsAsync(int? departmentId, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var query = dbContext.Doctors.AsNoTracking().Where(doctor => doctor.IsActive && doctor.User.IsActive && doctor.Department.IsActive);
        if (departmentId.HasValue) query = query.Where(doctor => doctor.DepartmentId == departmentId.Value);
        return await query.OrderBy(doctor => doctor.LastName).ThenBy(doctor => doctor.FirstName).Skip(pagination.Skip).Take(pagination.PageSize).Select(doctor => ToDto(doctor)).ToListAsync(cancellationToken);
    }

    public async Task<DoctorSummaryDto> GetDoctorAsync(int doctorId, CancellationToken cancellationToken) =>
        await dbContext.Doctors.AsNoTracking().Where(doctor => doctor.DoctorId == doctorId && doctor.IsActive && doctor.User.IsActive && doctor.Department.IsActive).Select(doctor => ToDto(doctor)).SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Doctor not found.");

    private static DepartmentDto ToDto(Department department) => new(department.DepartmentId, department.DepartmentCode, department.Name, department.Description, department.IsActive);
    private static DoctorSummaryDto ToDto(Doctor doctor) => new(doctor.DoctorId, doctor.DepartmentId, doctor.FirstName, doctor.LastName, doctor.Specialization, doctor.PhoneNumber, doctor.ConsultationFee);
}
