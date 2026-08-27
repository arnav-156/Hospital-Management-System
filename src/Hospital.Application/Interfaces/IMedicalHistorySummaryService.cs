using Hospital.Application.DTOs.Ai;

namespace Hospital.Application.Interfaces;

public interface IMedicalHistorySummaryService
{
    Task<MedicalHistorySummaryDto> GenerateAsync(int doctorUserId, int patientId, CancellationToken cancellationToken);
}
