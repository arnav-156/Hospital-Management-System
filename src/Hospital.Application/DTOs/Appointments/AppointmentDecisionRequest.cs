using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Appointments;

public sealed class AppointmentDecisionRequest { [StringLength(1000)] public string? Note { get; init; } }
