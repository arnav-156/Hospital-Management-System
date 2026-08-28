namespace Hospital.Application.DTOs.Treatments;

public sealed record TreatmentDto(int TreatmentId, int AppointmentId, int PatientId, int DoctorId, string? Diagnosis, string? Prescription, string? ProgressNotes, string? TreatmentNotes, DateTime TreatmentDateTime, string? DoctorName = null);
