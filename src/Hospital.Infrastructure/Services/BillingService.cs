using Hospital.Application.DTOs;
using Hospital.Application.DTOs.Billing;
using Hospital.Application.Exceptions;
using Hospital.Application.Interfaces;
using Hospital.Application.Rules;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public sealed class BillingService(HospitalManagementDbContext db, TimeProvider clock, INotificationService notifications) : IBillingService
{
    public async Task<BillDto> CreateAsync(int doctorUserId, int appointmentId, CreateBillRequest request, CancellationToken cancellationToken)
    {
        var doctor = await FindDoctorAsync(doctorUserId, cancellationToken);
        var appointment = await db.Appointments.SingleOrDefaultAsync(item => item.AppointmentId == appointmentId && item.DoctorId == doctor.DoctorId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");
        if (!AppointmentWorkflowRules.CanGenerateBill(appointment.Status)) throw new ConflictException("A bill can be generated only for a completed appointment.");
        if (request.DueDate < DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime)) throw new ConflictException("A bill due date cannot be in the past.");
        if (await db.Bills.AnyAsync(item => item.AppointmentId == appointmentId, cancellationToken)) throw new ConflictException("A bill already exists for this appointment.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var bill = new Bill
        {
            AppointmentId = appointmentId,
            PatientId = appointment.PatientId,
            GeneratedByDoctorId = doctor.DoctorId,
            Amount = request.Amount,
            Description = request.Description?.Trim(),
            DueDate = request.DueDate,
            Status = "Pending",
            GeneratedAt = clock.GetUtcNow().UtcDateTime,
        };
        db.Bills.Add(bill);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("A bill already exists for this appointment.");
        }

        var patientUserId = await PatientUserIdAsync(appointment.PatientId, cancellationToken);
        await notifications.CreateAsync(patientUserId, "BillGenerated", $"A bill of {bill.Amount:0.00} was generated for your appointment.", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(bill);
    }

    public async Task<BillDto> GetAsync(int patientUserId, int billId, CancellationToken cancellationToken)
    {
        var patient = await FindPatientAsync(patientUserId, cancellationToken);
        var bill = await db.Bills.AsNoTracking().SingleOrDefaultAsync(item => item.BillId == billId && item.PatientId == patient.PatientId, cancellationToken)
            ?? throw new NotFoundException("Bill not found.");
        return ToDto(bill);
    }

    public async Task<IReadOnlyList<BillDto>> GetMineAsync(int patientUserId, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var patient = await FindPatientAsync(patientUserId, cancellationToken);
        var bills = await db.Bills.AsNoTracking().Where(item => item.PatientId == patient.PatientId).OrderByDescending(item => item.GeneratedAt).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        return bills.Select(ToDto).ToList();
    }

    public async Task<BillDto> RecordPaymentAsync(int patientUserId, int billId, RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        var patient = await FindPatientAsync(patientUserId, cancellationToken);
        var bill = await db.Bills.SingleOrDefaultAsync(item => item.BillId == billId && item.PatientId == patient.PatientId, cancellationToken)
            ?? throw new NotFoundException("Bill not found.");
        if (bill.Status != "Pending") throw new ConflictException("Only a pending bill can be recorded as paid.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var paidAt = clock.GetUtcNow().UtcDateTime;
        bill.Status = "Paid";
        bill.PaidAt = paidAt;
        db.BillPayments.Add(new BillPayment
        {
            BillId = bill.BillId,
            RecordedByPatientId = patient.PatientId,
            Amount = bill.Amount,
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber) ? null : request.ReferenceNumber.Trim(),
            RecordedAt = paidAt,
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("A payment has already been recorded for this bill.");
        }

        if (bill.GeneratedByDoctorId is int doctorId)
        {
            var doctorUserId = await db.Doctors.Where(item => item.DoctorId == doctorId).Select(item => item.UserId).SingleOrDefaultAsync(cancellationToken);
            if (doctorUserId > 0)
            {
                await notifications.CreateAsync(doctorUserId, "BillPaid", $"A payment of {bill.Amount:0.00} was recorded for bill #{bill.BillId}.", cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return ToDto(bill);
    }

    public async Task<IReadOnlyList<PaymentDto>> GetPaymentHistoryAsync(int patientUserId, int billId, CancellationToken cancellationToken)
    {
        var patient = await FindPatientAsync(patientUserId, cancellationToken);
        var ownsBill = await db.Bills.AnyAsync(item => item.BillId == billId && item.PatientId == patient.PatientId, cancellationToken);
        if (!ownsBill) throw new NotFoundException("Bill not found.");
        return await PaymentHistoryAsync(billId, cancellationToken);
    }

    public async Task<IReadOnlyList<BillDto>> GetDoctorBillsAsync(int doctorUserId, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var doctor = await FindDoctorAsync(doctorUserId, cancellationToken);
        var bills = await db.Bills.AsNoTracking().Where(item => item.GeneratedByDoctorId == doctor.DoctorId).OrderByDescending(item => item.GeneratedAt).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        return bills.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<PaymentDto>> GetDoctorPaymentHistoryAsync(int doctorUserId, int billId, CancellationToken cancellationToken)
    {
        var doctor = await FindDoctorAsync(doctorUserId, cancellationToken);
        var ownsBill = await db.Bills.AnyAsync(item => item.BillId == billId && item.GeneratedByDoctorId == doctor.DoctorId, cancellationToken);
        if (!ownsBill) throw new NotFoundException("Bill not found.");
        return await PaymentHistoryAsync(billId, cancellationToken);
    }

    public async Task<BillDto> VoidAsync(int doctorUserId, int billId, VoidBillRequest request, CancellationToken cancellationToken)
    {
        var doctor = await FindDoctorAsync(doctorUserId, cancellationToken);
        var bill = await db.Bills.SingleOrDefaultAsync(item => item.BillId == billId && item.GeneratedByDoctorId == doctor.DoctorId, cancellationToken)
            ?? throw new NotFoundException("Bill not found.");
        if (bill.Status != "Pending") throw new ConflictException("Only a pending bill can be voided.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new ConflictException("A void reason is required.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        bill.Status = "Void";
        bill.VoidedAt = clock.GetUtcNow().UtcDateTime;
        bill.VoidedByDoctorId = doctor.DoctorId;
        bill.VoidReason = request.Reason.Trim();
        await db.SaveChangesAsync(cancellationToken);

        var patientUserId = await PatientUserIdAsync(bill.PatientId, cancellationToken);
        await notifications.CreateAsync(patientUserId, "BillVoided", $"Bill #{bill.BillId} was voided: {bill.VoidReason}", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(bill);
    }

    private async Task<Doctor> FindDoctorAsync(int userId, CancellationToken cancellationToken) =>
        await db.Doctors.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken) ?? throw new NotFoundException("Doctor profile not found.");

    private async Task<Patient> FindPatientAsync(int userId, CancellationToken cancellationToken) =>
        await db.Patients.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken) ?? throw new NotFoundException("Patient profile not found.");

    private async Task<int> PatientUserIdAsync(int patientId, CancellationToken cancellationToken) =>
        await db.Patients.Where(item => item.PatientId == patientId).Select(item => item.UserId).SingleAsync(cancellationToken);

    private async Task<IReadOnlyList<PaymentDto>> PaymentHistoryAsync(int billId, CancellationToken cancellationToken) =>
        (await db.BillPayments.AsNoTracking().Where(item => item.BillId == billId).OrderByDescending(item => item.RecordedAt).ToListAsync(cancellationToken)).Select(ToDto).ToList();

    private static BillDto ToDto(Bill bill) => new(bill.BillId, bill.AppointmentId, bill.PatientId, bill.GeneratedByDoctorId, bill.Amount, bill.Status, bill.Description, bill.GeneratedAt, bill.DueDate, bill.PaidAt, bill.VoidedAt, bill.VoidReason);

    private static PaymentDto ToDto(BillPayment payment) => new(payment.PaymentId, payment.BillId, payment.Amount, payment.PaymentMethod, payment.ReferenceNumber, payment.RecordedAt);
}
