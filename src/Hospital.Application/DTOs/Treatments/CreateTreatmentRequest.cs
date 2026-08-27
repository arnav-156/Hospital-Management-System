using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Treatments;

public sealed class CreateTreatmentRequest { [StringLength(1000)] public string? Diagnosis { get; init; } public string? Prescription { get; init; } public string? ProgressNotes { get; init; } public string? TreatmentNotes { get; init; } }
