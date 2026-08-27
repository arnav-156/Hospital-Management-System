using Hospital.Application.DTOs.Profiles;
using Hospital.Application.DTOs;
using Hospital.Application.Exceptions;
using Hospital.Application.Interfaces;
using Hospital.Application.Security;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public sealed class ProfileService(HospitalManagementDbContext dbContext, TimeProvider timeProvider) : IProfileService
{
    public async Task<object> GetCurrentProfileAsync(int userId, string role, CancellationToken cancellationToken) => role switch
    {
        UserRoles.Patient => await dbContext.Patients.AsNoTracking().Include(patient => patient.User).Where(patient => patient.UserId == userId).Select(patient => ToDto(patient)).SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Patient profile not found."),
        UserRoles.Doctor => await dbContext.Doctors.AsNoTracking().Include(doctor => doctor.User).Include(doctor => doctor.Department).Where(doctor => doctor.UserId == userId).Select(doctor => ToDto(doctor)).SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Doctor profile not found."),
        UserRoles.Administrator => await dbContext.Users.AsNoTracking().Where(user => user.UserId == userId).Select(user => new UserAccountDto(user.UserId, user.Email, user.Role, user.IsActive, user.CreatedAt)).SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Account not found."),
        _ => throw new NotFoundException("Profile not found."),
    };

    public async Task<PatientProfileDto> UpdatePatientProfileAsync(int userId, UpdatePatientProfileRequest request, CancellationToken cancellationToken)
    {
        if (request.DateOfBirth is null || request.DateOfBirth > DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
        {
            throw new ConflictException("Date of birth must not be in the future.");
        }

        var patient = await dbContext.Patients.Include(candidate => candidate.User).SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);
        if (patient is null)
        {
            patient = new Patient
            {
                UserId = userId,
                MedicalRecordNumber = $"PAT-{userId:D8}",
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            };
            dbContext.Patients.Add(patient);
        }

        patient.FirstName = request.FirstName.Trim();
        patient.LastName = request.LastName.Trim();
        patient.DateOfBirth = request.DateOfBirth.Value;
        patient.Gender = request.Gender?.Trim();
        patient.PhoneNumber = request.PhoneNumber?.Trim();
        patient.Address = request.Address?.Trim();
        patient.EmergencyContactName = request.EmergencyContactName?.Trim();
        patient.EmergencyContactPhone = request.EmergencyContactPhone?.Trim();
        patient.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(patient).Reference(candidate => candidate.User).LoadAsync(cancellationToken);
        return ToDto(patient);
    }

    public async Task<IReadOnlyList<PatientProfileDto>> GetPatientsAsync(string? search, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var query = dbContext.Patients.AsNoTracking().Include(patient => patient.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(patient => patient.FirstName.Contains(search) || patient.LastName.Contains(search) || patient.MedicalRecordNumber.Contains(search) || patient.User.Email.Contains(search));
        var patients = await query.OrderBy(patient => patient.LastName).ThenBy(patient => patient.FirstName).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        return patients.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<DoctorProfileDto>> GetDoctorsAsync(string? search, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var query = dbContext.Doctors.AsNoTracking().Include(doctor => doctor.User).Include(doctor => doctor.Department).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(doctor => doctor.FirstName.Contains(search) || doctor.LastName.Contains(search) || doctor.Specialization.Contains(search) || doctor.User.Email.Contains(search));
        var doctors = await query.OrderBy(doctor => doctor.LastName).ThenBy(doctor => doctor.FirstName).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        return doctors.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<StaffProfileDto>> GetStaffAsync(string? search, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var query = dbContext.Staff.AsNoTracking().Include(staff => staff.User).Include(staff => staff.Department).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(staff => staff.FirstName.Contains(search) || staff.LastName.Contains(search) || staff.EmployeeNumber.Contains(search) || staff.JobTitle.Contains(search) || staff.User.Email.Contains(search));
        var staff = await query.OrderBy(staff => staff.LastName).ThenBy(staff => staff.FirstName).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        return staff.Select(ToDto).ToList();
    }

    public async Task<UserAccountDto> UpdateAccountStatusAsync(int userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken) ?? throw new NotFoundException("Account not found.");
        user.IsActive = isActive;
        user.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new UserAccountDto(user.UserId, user.Email, user.Role, user.IsActive, user.CreatedAt);
    }

    private static PatientProfileDto ToDto(Patient patient) => new(patient.UserId, patient.PatientId, patient.User.Email, patient.MedicalRecordNumber, patient.FirstName, patient.LastName, patient.DateOfBirth, patient.Gender, patient.PhoneNumber, patient.Address, patient.EmergencyContactName, patient.EmergencyContactPhone);
    private static DoctorProfileDto ToDto(Doctor doctor) => new(doctor.UserId, doctor.DoctorId, doctor.User.Email, doctor.FirstName, doctor.LastName, doctor.LicenseNumber, doctor.Specialization, doctor.Department.Name, doctor.PhoneNumber, doctor.ConsultationFee, doctor.IsActive);
    private static StaffProfileDto ToDto(Staff staff) => new(staff.UserId, staff.StaffId, staff.User.Email, staff.FirstName, staff.LastName, staff.EmployeeNumber, staff.JobTitle, staff.Department?.Name, staff.PhoneNumber, staff.IsActive);
}
