using Hospital.Application.DTOs.Dashboard;
using Hospital.Application.Exceptions;
using Hospital.Application.Interfaces;
using Hospital.Application.Security;
using Hospital.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public sealed class DashboardService(HospitalManagementDbContext dbContext, TimeProvider timeProvider) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetAsync(int userId, string role, CancellationToken cancellationToken) => role switch
    {
        UserRoles.Patient => await GetPatientSummaryAsync(userId, cancellationToken),
        UserRoles.Doctor => await GetDoctorSummaryAsync(userId, cancellationToken),
        UserRoles.Administrator => await GetAdministratorSummaryAsync(cancellationToken),
        _ => throw new NotFoundException("Dashboard not found."),
    };

    private async Task<DashboardSummaryDto> GetPatientSummaryAsync(int userId, CancellationToken cancellationToken)
    {
        var patientId = await dbContext.Patients.AsNoTracking().Where(patient => patient.UserId == userId).Select(patient => (int?)patient.PatientId).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Patient profile not found.");
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new DashboardSummaryDto(
            await dbContext.Appointments.CountAsync(appointment => appointment.PatientId == patientId && appointment.AppointmentDateTime > now && (appointment.Status == "Pending" || appointment.Status == "Accepted"), cancellationToken),
            await dbContext.Notifications.CountAsync(notification => notification.UserId == userId && !notification.IsRead, cancellationToken),
            await dbContext.Bills.Where(bill => bill.PatientId == patientId && bill.Status == "Pending").SumAsync(bill => (decimal?)bill.Amount, cancellationToken) ?? 0m,
            0,
            0,
            0,
            0,
            0);
    }

    private async Task<DashboardSummaryDto> GetDoctorSummaryAsync(int userId, CancellationToken cancellationToken)
    {
        var doctorId = await dbContext.Doctors.AsNoTracking().Where(doctor => doctor.UserId == userId).Select(doctor => (int?)doctor.DoctorId).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Doctor profile not found.");
        var monthStart = new DateTime(timeProvider.GetUtcNow().UtcDateTime.Year, timeProvider.GetUtcNow().UtcDateTime.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        return new DashboardSummaryDto(
            0,
            await dbContext.Notifications.CountAsync(notification => notification.UserId == userId && !notification.IsRead, cancellationToken),
            0m,
            await dbContext.Appointments.CountAsync(appointment => appointment.DoctorId == doctorId && appointment.Status == "Pending", cancellationToken),
            await dbContext.Appointments.Where(appointment => appointment.DoctorId == doctorId && appointment.AppointmentDateTime >= monthStart && appointment.AppointmentDateTime < monthEnd && appointment.Status != "Rejected" && appointment.Status != "Cancelled").Select(appointment => appointment.PatientId).Distinct().CountAsync(cancellationToken),
            0,
            0,
            0);
    }

    private async Task<DashboardSummaryDto> GetAdministratorSummaryAsync(CancellationToken cancellationToken) => new(
        0,
        0,
        0m,
        0,
        0,
        await dbContext.Staff.CountAsync(staff => staff.User.IsActive, cancellationToken),
        await dbContext.Doctors.CountAsync(doctor => doctor.IsActive && doctor.User.IsActive, cancellationToken),
        await dbContext.Patients.CountAsync(cancellationToken));
}
