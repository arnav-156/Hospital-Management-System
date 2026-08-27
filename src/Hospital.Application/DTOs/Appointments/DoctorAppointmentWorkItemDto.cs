namespace Hospital.Application.DTOs.Appointments;

public sealed record DoctorAppointmentWorkItemDto(
    int AppointmentId,
    int PatientId,
    string PatientName,
    string MedicalRecordNumber,
    int DepartmentId,
    string DepartmentName,
    DateTime AppointmentDateTime,
    string Status,
    bool HasBill,
    string? Reason);
