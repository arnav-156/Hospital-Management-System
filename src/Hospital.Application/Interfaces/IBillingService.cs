using Hospital.Application.DTOs.Billing;
using Hospital.Application.DTOs;

namespace Hospital.Application.Interfaces;

public interface IBillingService { Task<BillDto> CreateAsync(int doctorUserId, int appointmentId, CreateBillRequest request, CancellationToken cancellationToken); Task<BillDto> GetAsync(int patientUserId, int billId, CancellationToken cancellationToken); Task<IReadOnlyList<BillDto>> GetMineAsync(int patientUserId, PaginationRequest pagination, CancellationToken cancellationToken); }
