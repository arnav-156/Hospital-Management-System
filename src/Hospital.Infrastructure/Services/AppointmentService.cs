using Hospital.Application.DTOs.Appointments;
using Hospital.Application.DTOs;
using Hospital.Application.Exceptions;
using Hospital.Application.Interfaces;
using Hospital.Application.Rules;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public sealed class AppointmentService(HospitalManagementDbContext db, TimeProvider clock, INotificationService notifications) : IAppointmentService
{
    public async Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(int doctorId, DateOnly appointmentDate, CancellationToken ct)
    {
        var doctorExists = await db.Doctors.AnyAsync(d => d.DoctorId == doctorId && d.IsActive && d.User.IsActive && d.Department.IsActive, ct);
        if (!doctorExists) throw new NotFoundException("Doctor not found.");
        var day = appointmentDate.ToDateTime(TimeOnly.MinValue); var booked = await db.Appointments.Where(a => a.DoctorId == doctorId && a.AppointmentDateTime >= day && a.AppointmentDateTime < day.AddDays(1) && a.Status != "Rejected" && a.Status != "Cancelled").Select(a => a.AppointmentDateTime).ToListAsync(ct);
        return Enumerable.Range(0, 16).Select(i => day.AddHours(9).AddMinutes(i * 30)).Where(slot => slot > clock.GetUtcNow().UtcDateTime && !booked.Contains(slot)).ToList();
    }
    public async Task<AppointmentDto> CreateAsync(int userId, CreateAppointmentRequest request, CancellationToken ct)
    {
        var patient = await db.Patients.SingleOrDefaultAsync(p => p.UserId == userId, ct) ?? throw new ConflictException("Complete your patient profile before requesting an appointment.");
        var slot = DateTime.SpecifyKind(request.AppointmentDateTime, DateTimeKind.Utc);
        if (!AppointmentWorkflowRules.IsBookableSlot(slot, clock.GetUtcNow().UtcDateTime)) throw new ConflictException("Choose a future half-hour slot between 09:00 and 17:00 UTC.");
        var doctor = await db.Doctors.SingleOrDefaultAsync(d => d.DoctorId == request.DoctorId && d.DepartmentId == request.DepartmentId && d.IsActive && d.User.IsActive && d.Department.IsActive, ct) ?? throw new NotFoundException("Active doctor in the selected department not found.");
        var appointment = new Appointment { PatientId = patient.PatientId, DoctorId = doctor.DoctorId, DepartmentId = doctor.DepartmentId, AppointmentDateTime = slot, DurationMinutes = 30, Status = "Pending", Reason = request.Reason?.Trim(), CreatedAt = clock.GetUtcNow().UtcDateTime };
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.Appointments.Add(appointment);
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { throw new ConflictException("That doctor slot is no longer available."); }
        await notifications.CreateAsync(doctor.UserId, "AppointmentRequested", $"New appointment request for {slot:yyyy-MM-dd HH:mm} UTC.", ct);
        await transaction.CommitAsync(ct);
        return ToDto(appointment);
    }
    public async Task<IReadOnlyList<AppointmentDto>> GetPatientAppointmentsAsync(int userId, PaginationRequest pagination, CancellationToken ct) { var patient = await db.Patients.SingleOrDefaultAsync(p => p.UserId == userId, ct) ?? throw new NotFoundException("Patient profile not found."); return (await db.Appointments.AsNoTracking().Where(a => a.PatientId == patient.PatientId).OrderBy(a => a.AppointmentDateTime).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(ct)).Select(ToDto).ToList(); }
    public async Task<IReadOnlyList<PatientAppointmentSummaryDto>> GetPatientAppointmentSummariesAsync(int userId, PaginationRequest pagination, CancellationToken ct)
    {
        var patient = await db.Patients.SingleOrDefaultAsync(p => p.UserId == userId, ct) ?? throw new NotFoundException("Patient profile not found.");
        return await db.Appointments.AsNoTracking().Where(appointment => appointment.PatientId == patient.PatientId).OrderBy(appointment => appointment.AppointmentDateTime).Skip(pagination.Skip).Take(pagination.PageSize).Select(appointment => new PatientAppointmentSummaryDto(appointment.AppointmentId, appointment.AppointmentDateTime, appointment.Status, appointment.Doctor.FirstName + " " + appointment.Doctor.LastName, appointment.Doctor.Department.Name, appointment.Reason, appointment.DoctorResponseNote)).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<AppointmentDto>> GetPatientFeedbackEligibleAppointmentsAsync(int userId, PaginationRequest pagination, CancellationToken ct)
    {
        var patient = await db.Patients.SingleOrDefaultAsync(p => p.UserId == userId, ct) ?? throw new NotFoundException("Patient profile not found.");
        return (await db.Appointments.AsNoTracking().Where(appointment => appointment.PatientId == patient.PatientId && appointment.Status == "Completed" && appointment.Feedbacks.Count == 0).OrderByDescending(appointment => appointment.AppointmentDateTime).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(ct)).Select(ToDto).ToList();
    }
    public async Task<AppointmentDto> GetPatientAppointmentAsync(int userId, int appointmentId, CancellationToken ct) { var patient = await db.Patients.SingleOrDefaultAsync(p => p.UserId == userId, ct) ?? throw new NotFoundException("Patient profile not found."); return ToDto(await db.Appointments.AsNoTracking().SingleOrDefaultAsync(a => a.AppointmentId == appointmentId && a.PatientId == patient.PatientId, ct) ?? throw new NotFoundException("Appointment not found.")); }
    public async Task<AppointmentDto> CancelAsync(int userId, int appointmentId, CancellationToken ct)
    {
        var patient = await db.Patients.SingleOrDefaultAsync(candidate => candidate.UserId == userId, ct) ?? throw new NotFoundException("Patient profile not found.");
        var appointment = await db.Appointments.SingleOrDefaultAsync(candidate => candidate.AppointmentId == appointmentId && candidate.PatientId == patient.PatientId, ct) ?? throw new NotFoundException("Appointment not found.");
        var now = clock.GetUtcNow().UtcDateTime;
        if (!AppointmentWorkflowRules.CanCancel(appointment.Status) || appointment.AppointmentDateTime <= now) throw new ConflictException("Only future pending or accepted appointments can be cancelled.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        appointment.Status = "Cancelled";
        appointment.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        var doctorUserId = await db.Doctors.Where(doctor => doctor.DoctorId == appointment.DoctorId).Select(doctor => doctor.UserId).SingleAsync(ct);
        await notifications.CreateAsync(doctorUserId, "AppointmentCancelled", $"The appointment on {appointment.AppointmentDateTime:yyyy-MM-dd HH:mm} UTC was cancelled by the patient.", ct);
        await transaction.CommitAsync(ct);
        return ToDto(appointment);
    }
    public async Task<IReadOnlyList<AppointmentDto>> GetDoctorAppointmentsAsync(int userId, bool today, PaginationRequest pagination, CancellationToken ct) { var doctor = await db.Doctors.SingleOrDefaultAsync(d => d.UserId == userId, ct) ?? throw new NotFoundException("Doctor profile not found."); var q = db.Appointments.AsNoTracking().Where(a => a.DoctorId == doctor.DoctorId); if (today) { var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).ToDateTime(TimeOnly.MinValue); q = q.Where(a => a.AppointmentDateTime >= day && a.AppointmentDateTime < day.AddDays(1)); } else q = q.Where(a => a.Status == "Pending"); return (await q.OrderBy(a => a.AppointmentDateTime).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(ct)).Select(ToDto).ToList(); }
    public async Task<IReadOnlyList<DoctorAppointmentWorkItemDto>> GetDoctorWorkItemsAsync(int userId, PaginationRequest pagination, CancellationToken ct)
    {
        return await GetDoctorWorkItemsAsync(userId, pagination, false, ct);
    }
    public async Task<IReadOnlyList<DoctorAppointmentWorkItemDto>> GetDoctorPendingWorkItemsAsync(int userId, PaginationRequest pagination, CancellationToken ct)
    {
        return await GetDoctorWorkItemsAsync(userId, pagination, true, ct);
    }
    public async Task<IReadOnlyList<DoctorAppointmentWorkItemDto>> GetDoctorTodayWorkItemsAsync(int userId, PaginationRequest pagination, CancellationToken ct)
    {
        var doctor = await db.Doctors.SingleOrDefaultAsync(d => d.UserId == userId, ct) ?? throw new NotFoundException("Doctor profile not found.");
        var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).ToDateTime(TimeOnly.MinValue);
        return await db.Appointments.AsNoTracking().Where(appointment => appointment.DoctorId == doctor.DoctorId && appointment.AppointmentDateTime >= day && appointment.AppointmentDateTime < day.AddDays(1) && appointment.Status != "Rejected" && appointment.Status != "Cancelled").OrderBy(appointment => appointment.AppointmentDateTime).Skip(pagination.Skip).Take(pagination.PageSize).Select(appointment => new DoctorAppointmentWorkItemDto(appointment.AppointmentId, appointment.PatientId, appointment.Patient.FirstName + " " + appointment.Patient.LastName, appointment.Patient.MedicalRecordNumber, appointment.DepartmentId, appointment.Doctor.Department.Name, appointment.AppointmentDateTime, appointment.Status, appointment.Bills.Count > 0, appointment.Reason)).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<DoctorAppointmentWorkItemDto>> GetDoctorUpcomingWorkItemsAsync(int userId, PaginationRequest pagination, CancellationToken ct)
    {
        var doctor = await db.Doctors.SingleOrDefaultAsync(candidate => candidate.UserId == userId, ct) ?? throw new NotFoundException("Doctor profile not found.");
        var now = clock.GetUtcNow().UtcDateTime;
        return await db.Appointments.AsNoTracking().Where(appointment => appointment.DoctorId == doctor.DoctorId && appointment.AppointmentDateTime > now && appointment.Status == "Accepted").OrderBy(appointment => appointment.AppointmentDateTime).Skip(pagination.Skip).Take(pagination.PageSize).Select(appointment => new DoctorAppointmentWorkItemDto(appointment.AppointmentId, appointment.PatientId, appointment.Patient.FirstName + " " + appointment.Patient.LastName, appointment.Patient.MedicalRecordNumber, appointment.DepartmentId, appointment.Doctor.Department.Name, appointment.AppointmentDateTime, appointment.Status, appointment.Bills.Count > 0, appointment.Reason)).ToListAsync(ct);
    }
    private async Task<IReadOnlyList<DoctorAppointmentWorkItemDto>> GetDoctorWorkItemsAsync(int userId, PaginationRequest pagination, bool pendingOnly, CancellationToken ct)
    {
        var doctor = await db.Doctors.SingleOrDefaultAsync(d => d.UserId == userId, ct) ?? throw new NotFoundException("Doctor profile not found.");
        var appointments = db.Appointments.AsNoTracking().Where(appointment => appointment.DoctorId == doctor.DoctorId);
        appointments = pendingOnly
            ? appointments.Where(appointment => appointment.Status == "Pending")
            : appointments.Where(appointment => appointment.Status != "Rejected" && appointment.Status != "Cancelled");
        var orderedAppointments = pendingOnly
            ? appointments.OrderBy(appointment => appointment.AppointmentDateTime)
            : appointments.OrderByDescending(appointment => appointment.AppointmentDateTime);
        return await orderedAppointments
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(appointment => new DoctorAppointmentWorkItemDto(
                appointment.AppointmentId,
                appointment.PatientId,
                appointment.Patient.FirstName + " " + appointment.Patient.LastName,
                appointment.Patient.MedicalRecordNumber,
                appointment.DepartmentId,
                appointment.Doctor.Department.Name,
                appointment.AppointmentDateTime,
                appointment.Status,
                appointment.Bills.Count > 0,
                appointment.Reason))
            .ToListAsync(ct);
    }
    public async Task<AppointmentDto> DecideAsync(int userId, int appointmentId, bool accepted, string? note, CancellationToken ct) { var doctor = await db.Doctors.SingleOrDefaultAsync(d => d.UserId == userId, ct) ?? throw new NotFoundException("Doctor profile not found."); var a = await db.Appointments.SingleOrDefaultAsync(a => a.AppointmentId == appointmentId && a.DoctorId == doctor.DoctorId, ct) ?? throw new NotFoundException("Appointment not found."); if (!AppointmentWorkflowRules.CanReview(a.Status)) throw new ConflictException("Only pending appointments can be reviewed."); var now = clock.GetUtcNow().UtcDateTime; if (a.AppointmentDateTime <= now) throw new ConflictException("An appointment can be reviewed only before its scheduled time."); await using var transaction = await db.Database.BeginTransactionAsync(ct); a.Status = accepted ? "Accepted" : "Rejected"; a.DoctorResponseNote = note?.Trim(); a.UpdatedAt = now; await db.SaveChangesAsync(ct); var patientUserId = await db.Patients.Where(p => p.PatientId == a.PatientId).Select(p => p.UserId).SingleAsync(ct); await notifications.CreateAsync(patientUserId, accepted ? "AppointmentAccepted" : "AppointmentRejected", $"Your appointment on {a.AppointmentDateTime:yyyy-MM-dd HH:mm} UTC was {a.Status.ToLowerInvariant()}.", ct); await transaction.CommitAsync(ct); return ToDto(a); }
    private static AppointmentDto ToDto(Appointment a) => new(a.AppointmentId, a.PatientId, a.DoctorId, a.DepartmentId, a.AppointmentDateTime, a.DurationMinutes, a.Status, a.Reason, a.DoctorResponseNote);
}
