using Hospital.Application.DTOs.Profiles;
using Hospital.Application.DTOs;
using Hospital.Application.Exceptions;
using Hospital.Application.Interfaces;
using Hospital.Application.Security;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public sealed class ProfileService(HospitalManagementDbContext dbContext, TimeProvider timeProvider, IPasswordHasher<User> passwordHasher) : IProfileService
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

        ApplyPatientUpdate(patient, request);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(patient).Reference(candidate => candidate.User).LoadAsync(cancellationToken);
        return ToDto(patient);
    }

    public async Task<DoctorProfileDto> UpdateDoctorOwnProfileAsync(int userId, UpdateDoctorOwnProfileRequest request, CancellationToken cancellationToken)
    {
        var doctor = await dbContext.Doctors.Include(candidate => candidate.User).Include(candidate => candidate.Department).SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken) ?? throw new NotFoundException("Doctor profile not found.");
        doctor.FirstName = request.FirstName.Trim();
        doctor.LastName = request.LastName.Trim();
        doctor.Specialization = request.Specialization.Trim();
        doctor.PhoneNumber = request.PhoneNumber?.Trim();
        doctor.ConsultationFee = request.ConsultationFee;
        doctor.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(doctor);
    }

    public async Task<PatientProfileDto> UpdatePatientAsync(int patientId, UpdatePatientProfileRequest request, CancellationToken cancellationToken)
    {
        var patient = await dbContext.Patients.Include(candidate => candidate.User).SingleOrDefaultAsync(candidate => candidate.PatientId == patientId, cancellationToken) ?? throw new NotFoundException("Patient profile not found.");
        ApplyPatientUpdate(patient, request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(patient);
    }

    public async Task<DoctorProfileDto> CreateDoctorAsync(CreateDoctorProfileRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var licenseNumber = request.LicenseNumber.Trim();
        if (await dbContext.Users.AnyAsync(candidate => candidate.Email == email, cancellationToken))
        {
            throw new ConflictException("An account with this email address already exists.");
        }
        if (await dbContext.Doctors.AnyAsync(candidate => candidate.LicenseNumber == licenseNumber, cancellationToken))
        {
            throw new ConflictException("Another doctor already uses that license number.");
        }

        var department = await dbContext.Departments.SingleOrDefaultAsync(candidate => candidate.DepartmentId == request.DepartmentId && candidate.IsActive, cancellationToken)
            ?? throw new NotFoundException("Active department not found.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = new User { Email = email, Role = UserRoles.Doctor, IsActive = true, CreatedAt = now };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var doctor = new Doctor
        {
            UserId = user.UserId,
            User = user,
            DepartmentId = department.DepartmentId,
            Department = department,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            LicenseNumber = licenseNumber,
            Specialization = request.Specialization.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            ConsultationFee = request.ConsultationFee,
            IsActive = request.IsActive,
            CreatedAt = now,
        };
        dbContext.Doctors.Add(doctor);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("A doctor account with this email address or license number already exists.");
        }

        await transaction.CommitAsync(cancellationToken);
        return ToDto(doctor);
    }

    public async Task<DoctorProfileDto> UpdateDoctorAsync(int doctorId, UpdateDoctorProfileRequest request, CancellationToken cancellationToken)
    {
        var doctor = await dbContext.Doctors.Include(candidate => candidate.User).Include(candidate => candidate.Department).SingleOrDefaultAsync(candidate => candidate.DoctorId == doctorId, cancellationToken) ?? throw new NotFoundException("Doctor profile not found.");
        var department = await dbContext.Departments.SingleOrDefaultAsync(candidate => candidate.DepartmentId == request.DepartmentId && candidate.IsActive, cancellationToken) ?? throw new NotFoundException("Active department not found.");
        var licenseNumber = request.LicenseNumber.Trim();
        if (await dbContext.Doctors.AnyAsync(candidate => candidate.DoctorId != doctorId && candidate.LicenseNumber == licenseNumber, cancellationToken))
        {
            throw new ConflictException("Another doctor already uses that license number.");
        }

        doctor.FirstName = request.FirstName.Trim();
        doctor.LastName = request.LastName.Trim();
        doctor.LicenseNumber = licenseNumber;
        doctor.Specialization = request.Specialization.Trim();
        doctor.DepartmentId = department.DepartmentId;
        doctor.Department = department;
        doctor.PhoneNumber = request.PhoneNumber?.Trim();
        doctor.ConsultationFee = request.ConsultationFee;
        doctor.IsActive = request.IsActive;
        doctor.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(doctor);
    }

    public async Task<StaffProfileDto> UpdateStaffAsync(int staffId, UpdateStaffProfileRequest request, CancellationToken cancellationToken)
    {
        var staff = await dbContext.Staff.Include(candidate => candidate.User).Include(candidate => candidate.Department).SingleOrDefaultAsync(candidate => candidate.StaffId == staffId, cancellationToken) ?? throw new NotFoundException("Staff profile not found.");
        Department? department = null;
        if (request.DepartmentId.HasValue)
        {
            department = await dbContext.Departments.SingleOrDefaultAsync(candidate => candidate.DepartmentId == request.DepartmentId.Value && candidate.IsActive, cancellationToken) ?? throw new NotFoundException("Active department not found.");
        }

        var employeeNumber = request.EmployeeNumber.Trim();
        if (await dbContext.Staff.AnyAsync(candidate => candidate.StaffId != staffId && candidate.EmployeeNumber == employeeNumber, cancellationToken))
        {
            throw new ConflictException("Another staff member already uses that employee number.");
        }

        staff.FirstName = request.FirstName.Trim();
        staff.LastName = request.LastName.Trim();
        staff.EmployeeNumber = employeeNumber;
        staff.JobTitle = request.JobTitle.Trim();
        staff.DepartmentId = department?.DepartmentId;
        staff.Department = department;
        staff.PhoneNumber = request.PhoneNumber?.Trim();
        staff.IsActive = request.IsActive;
        staff.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(staff);
    }

    private void ApplyPatientUpdate(Patient patient, UpdatePatientProfileRequest request)
    {
        if (request.DateOfBirth is null || request.DateOfBirth > DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
        {
            throw new ConflictException("Date of birth must not be in the future.");
        }

        patient.FirstName = request.FirstName.Trim();
        patient.LastName = request.LastName.Trim();
        patient.DateOfBirth = request.DateOfBirth.Value;
        patient.Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();
        patient.PhoneNumber = request.PhoneNumber?.Trim();
        patient.Address = request.Address?.Trim();
        patient.EmergencyContactName = request.EmergencyContactName?.Trim();
        patient.EmergencyContactPhone = request.EmergencyContactPhone?.Trim();
        patient.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
    }

    public async Task<IReadOnlyList<PatientProfileDto>> GetPatientsAsync(string? search, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var query = dbContext.Patients.AsNoTracking().Include(patient => patient.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(patient => patient.FirstName.Contains(search) || patient.LastName.Contains(search) || (patient.FirstName + " " + patient.LastName).Contains(search) || patient.MedicalRecordNumber.Contains(search) || patient.User.Email.Contains(search));
        var patients = await query.OrderBy(patient => patient.LastName).ThenBy(patient => patient.FirstName).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        return patients.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<DoctorProfileDto>> GetDoctorsAsync(string? search, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var query = dbContext.Doctors.AsNoTracking().Include(doctor => doctor.User).Include(doctor => doctor.Department).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(doctor => doctor.FirstName.Contains(search) || doctor.LastName.Contains(search) || (doctor.FirstName + " " + doctor.LastName).Contains(search) || doctor.Specialization.Contains(search) || doctor.User.Email.Contains(search));
        var doctors = await query.OrderBy(doctor => doctor.LastName).ThenBy(doctor => doctor.FirstName).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        return doctors.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<StaffProfileDto>> GetStaffAsync(string? search, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var query = dbContext.Staff.AsNoTracking().Include(staff => staff.User).Include(staff => staff.Department).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(staff => staff.FirstName.Contains(search) || staff.LastName.Contains(search) || (staff.FirstName + " " + staff.LastName).Contains(search) || staff.EmployeeNumber.Contains(search) || staff.JobTitle.Contains(search) || staff.User.Email.Contains(search));
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

    private static PatientProfileDto ToDto(Patient patient) => new(patient.UserId, patient.PatientId, patient.User.Email, patient.MedicalRecordNumber, patient.FirstName, patient.LastName, patient.DateOfBirth, patient.Gender, patient.PhoneNumber, patient.Address, patient.EmergencyContactName, patient.EmergencyContactPhone, patient.User.IsActive);
    private static DoctorProfileDto ToDto(Doctor doctor) => new(doctor.UserId, doctor.DoctorId, doctor.User.Email, doctor.FirstName, doctor.LastName, doctor.LicenseNumber, doctor.Specialization, doctor.DepartmentId, doctor.Department.Name, doctor.PhoneNumber, doctor.ConsultationFee, doctor.IsActive, doctor.User.IsActive);
    private static StaffProfileDto ToDto(Staff staff) => new(staff.UserId, staff.StaffId, staff.User.Email, staff.FirstName, staff.LastName, staff.EmployeeNumber, staff.JobTitle, staff.DepartmentId, staff.Department?.Name, staff.PhoneNumber, staff.IsActive, staff.User.IsActive);
}
