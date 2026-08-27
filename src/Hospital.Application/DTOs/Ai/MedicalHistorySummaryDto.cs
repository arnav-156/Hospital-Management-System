using Hospital.Application.DTOs.Treatments;

namespace Hospital.Application.DTOs.Ai;

public sealed record MedicalHistorySummaryDto(
    int PatientId,
    IReadOnlyList<TreatmentDto> History,
    bool AiAvailable,
    bool IsAiGenerated,
    string? Summary,
    string Disclaimer,
    DateTime GeneratedAtUtc);
