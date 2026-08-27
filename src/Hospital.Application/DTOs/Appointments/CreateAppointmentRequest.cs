using System.ComponentModel.DataAnnotations;
namespace Hospital.Application.DTOs.Appointments;
public sealed class CreateAppointmentRequest { [Range(1, int.MaxValue)] public int DoctorId { get; init; } [Range(1, int.MaxValue)] public int DepartmentId { get; init; } public DateTime AppointmentDateTime { get; init; } [StringLength(1000)] public string? Reason { get; init; } }
