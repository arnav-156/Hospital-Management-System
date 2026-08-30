using Hospital.Application.DTOs.Billing;
using Hospital.Application.DTOs;

namespace Hospital.Application.Interfaces;

public interface IBillingService { Task<BillDto> CreateAsync(int doctorUserId, int appointmentId, CreateBillRequest request, CancellationToken cancellationToken); Task<BillDto> GetAsync(int patientUserId, int billId, CancellationToken cancellationToken); Task<IReadOnlyList<BillDto>> GetMineAsync(int patientUserId, PaginationRequest pagination, CancellationToken cancellationToken); Task<BillDto> RecordPaymentAsync(int patientUserId, int billId, RecordPaymentRequest request, CancellationToken cancellationToken); Task<IReadOnlyList<PaymentDto>> GetPaymentHistoryAsync(int patientUserId, int billId, CancellationToken cancellationToken); Task<IReadOnlyList<BillDto>> GetDoctorBillsAsync(int doctorUserId, PaginationRequest pagination, CancellationToken cancellationToken); Task<IReadOnlyList<PaymentDto>> GetDoctorPaymentHistoryAsync(int doctorUserId, int billId, CancellationToken cancellationToken); Task<BillDto> VoidAsync(int doctorUserId, int billId, VoidBillRequest request, CancellationToken cancellationToken); }
