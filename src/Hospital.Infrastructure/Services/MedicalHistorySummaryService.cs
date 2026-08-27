using Hospital.Application.DTOs.Ai;
using Hospital.Application.DTOs.Treatments;
using Hospital.Application.Exceptions;
using Hospital.Application.Interfaces;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public sealed class MedicalHistorySummaryService(
    HospitalManagementDbContext dbContext,
    IClinicalSummaryGenerator generator,
    TimeProvider timeProvider) : IMedicalHistorySummaryService
{
    private const string Disclaimer = "AI-generated summary for doctor review only. It is not a medical diagnosis and does not prescribe or modify any medical record.";

    public async Task<MedicalHistorySummaryDto> GenerateAsync(int doctorUserId, int patientId, CancellationToken cancellationToken)
    {
        var doctor = await dbContext.Doctors.SingleOrDefaultAsync(candidate => candidate.UserId == doctorUserId, cancellationToken)
            ?? throw new NotFoundException("Doctor profile not found.");
        var authorized = await dbContext.Appointments.AnyAsync(appointment => appointment.PatientId == patientId && appointment.DoctorId == doctor.DoctorId, cancellationToken);
        if (!authorized) throw new NotFoundException("Patient history not found.");

        var history = (await dbContext.Treatments.AsNoTracking()
            .Where(treatment => treatment.PatientId == patientId)
            .OrderByDescending(treatment => treatment.TreatmentDateTime)
            .ToListAsync(cancellationToken))
            .Select(ToDto)
            .ToList();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (history.Count == 0)
        {
            await AuditAsync(patientId, doctor.DoctorId, now, "NoRecords", null, 0, "NoRecords", cancellationToken);
            return new MedicalHistorySummaryDto(patientId, history, false, false, null, "No treatment records are available. " + Disclaimer, now);
        }

        var generated = await generator.GenerateAsync(history, cancellationToken);
        var outcome = generated.IsAvailable ? "Generated" : "Unavailable";
        await AuditAsync(patientId, doctor.DoctorId, now, outcome, generated.Model, history.Count, generated.FailureCode, cancellationToken);
        return new MedicalHistorySummaryDto(
            patientId,
            history,
            generated.IsAvailable,
            generated.IsAvailable,
            generated.Summary,
            generated.IsAvailable ? Disclaimer : "AI summary is currently unavailable. Review the normal patient history below. " + Disclaimer,
            now);
    }

    private async Task AuditAsync(int patientId, int doctorId, DateTime requestedAt, string outcome, string? model, int recordCount, string? failureCode, CancellationToken cancellationToken)
    {
        dbContext.AiSummaryAudits.Add(new AiSummaryAudit { PatientId = patientId, DoctorId = doctorId, RequestedAt = requestedAt, Outcome = outcome, Model = model, RecordCount = recordCount, FailureCode = failureCode });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static TreatmentDto ToDto(Treatment treatment) => new(treatment.TreatmentId, treatment.AppointmentId, treatment.PatientId, treatment.DoctorId, treatment.Diagnosis, treatment.Prescription, treatment.ProgressNotes, treatment.TreatmentNotes, treatment.TreatmentDateTime);
}
