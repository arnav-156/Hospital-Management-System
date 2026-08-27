using Hospital.Application.DTOs.Treatments;
using Hospital.Application.DTOs;

namespace Hospital.Application.Interfaces;

public interface ITreatmentService { Task<TreatmentDto> CreateAsync(int doctorUserId, int appointmentId, CreateTreatmentRequest request, CancellationToken cancellationToken); Task<IReadOnlyList<TreatmentDto>> GetPatientHistoryAsync(int requestingUserId, string role, int patientId, PaginationRequest pagination, CancellationToken cancellationToken); }
