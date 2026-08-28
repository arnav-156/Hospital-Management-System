using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Treatments;

public sealed class CreateTreatmentRequest { [Required, StringLength(1000), RegularExpression(@".*\S.*", ErrorMessage = "Diagnosis cannot be blank.")] public string Diagnosis { get; init; } = string.Empty; public string? Prescription { get; init; } public string? ProgressNotes { get; init; } public string? TreatmentNotes { get; init; } }
