namespace Hospital.Application.DTOs.Appointments;
public sealed record AppointmentDto(int AppointmentId, int PatientId, int DoctorId, int DepartmentId, DateTime AppointmentDateTime, short DurationMinutes, string Status, string? Reason, string? DoctorResponseNote);
