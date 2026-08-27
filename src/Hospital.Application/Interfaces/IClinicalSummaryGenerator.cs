using Hospital.Application.DTOs.Ai;
using Hospital.Application.DTOs.Treatments;

namespace Hospital.Application.Interfaces;

public interface IClinicalSummaryGenerator
{
    Task<ClinicalSummaryGeneration> GenerateAsync(IReadOnlyList<TreatmentDto> history, CancellationToken cancellationToken);
}
