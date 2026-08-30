using Hospital.Application.DTOs.Treatments;
using Hospital.Application.DTOs;
using Hospital.Application.Exceptions;
using Hospital.Application.Interfaces;
using Hospital.Application.Security;
using Hospital.Application.Rules;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public sealed class TreatmentService(HospitalManagementDbContext dbContext, TimeProvider timeProvider) : ITreatmentService
{
    public async Task<TreatmentDto> CreateAsync(int doctorUserId, int appointmentId, CreateTreatmentRequest request, CancellationToken cancellationToken)
    {
        var doctor = await dbContext.Doctors.SingleOrDefaultAsync(candidate => candidate.UserId == doctorUserId, cancellationToken) ?? throw new NotFoundException("Doctor profile not found.");
        var appointment = await dbContext.Appointments.SingleOrDefaultAsync(candidate => candidate.AppointmentId == appointmentId && candidate.DoctorId == doctor.DoctorId, cancellationToken) ?? throw new NotFoundException("Appointment not found.");
        if (!AppointmentWorkflowRules.CanRecordTreatment(appointment.Status)) throw new ConflictException("Treatment can be recorded only for an accepted appointment.");
        if (await dbContext.Treatments.AnyAsync(candidate => candidate.AppointmentId == appointmentId, cancellationToken)) throw new ConflictException("A treatment already exists for this appointment.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (appointment.AppointmentDateTime > now) throw new ConflictException("Treatment can be recorded only after the scheduled appointment time.");
        var treatment = new Treatment { AppointmentId = appointment.AppointmentId, PatientId = appointment.PatientId, DoctorId = doctor.DoctorId, Diagnosis = request.Diagnosis?.Trim(), Prescription = request.Prescription?.Trim(), ProgressNotes = request.ProgressNotes?.Trim(), TreatmentNotes = request.TreatmentNotes?.Trim(), TreatmentDateTime = now, CreatedAt = now };
        dbContext.Treatments.Add(treatment); appointment.Status = "Completed"; appointment.UpdatedAt = now;
        try { await dbContext.SaveChangesAsync(cancellationToken); } catch (DbUpdateException) { throw new ConflictException("A treatment already exists for this appointment."); }
        return ToDto(treatment);
    }
    public async Task<IReadOnlyList<TreatmentDto>> GetPatientHistoryAsync(int requestingUserId, string role, int patientId, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        if (role == UserRoles.Patient)
        {
            var owns = await dbContext.Patients.AnyAsync(patient => patient.PatientId == patientId && patient.UserId == requestingUserId, cancellationToken);
            if (!owns) throw new NotFoundException("Patient history not found.");
        }
        else if (role == UserRoles.Doctor)
        {
            var doctor = await dbContext.Doctors.SingleOrDefaultAsync(candidate => candidate.UserId == requestingUserId, cancellationToken) ?? throw new NotFoundException("Doctor profile not found.");
            var authorized = await dbContext.Appointments.AnyAsync(appointment => appointment.PatientId == patientId && appointment.DoctorId == doctor.DoctorId && (appointment.Status == "Accepted" || appointment.Status == "Completed"), cancellationToken);
            if (!authorized) throw new NotFoundException("Patient history not found.");
        }
        else throw new NotFoundException("Patient history not found.");
        return await dbContext.Treatments.AsNoTracking().Where(treatment => treatment.PatientId == patientId).OrderByDescending(treatment => treatment.TreatmentDateTime).Skip(pagination.Skip).Take(pagination.PageSize).Select(treatment => new TreatmentDto(treatment.TreatmentId, treatment.AppointmentId, treatment.PatientId, treatment.DoctorId, treatment.Diagnosis, treatment.Prescription, treatment.ProgressNotes, treatment.TreatmentNotes, treatment.TreatmentDateTime, treatment.Appointment.Doctor.FirstName + " " + treatment.Appointment.Doctor.LastName)).ToListAsync(cancellationToken);
    }
    private static TreatmentDto ToDto(Treatment treatment) => new(treatment.TreatmentId, treatment.AppointmentId, treatment.PatientId, treatment.DoctorId, treatment.Diagnosis, treatment.Prescription, treatment.ProgressNotes, treatment.TreatmentNotes, treatment.TreatmentDateTime);
}
