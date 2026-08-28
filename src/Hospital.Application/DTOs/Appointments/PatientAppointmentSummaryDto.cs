namespace Hospital.Application.DTOs.Appointments;

public sealed record PatientAppointmentSummaryDto(
    int AppointmentId,
    DateTime AppointmentDateTime,
    string Status,
    string DoctorName,
    string DepartmentName,
    string? Reason,
    string? DoctorResponseNote);
