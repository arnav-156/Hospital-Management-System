using Hospital.Application.DTOs.Billing;
using Hospital.Application.DTOs;
using Hospital.Application.Exceptions;
using Hospital.Application.Interfaces;
using Hospital.Application.Rules;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public sealed class BillingService(HospitalManagementDbContext db, TimeProvider clock, INotificationService notifications) : IBillingService
{
    public async Task<BillDto> CreateAsync(int doctorUserId, int appointmentId, CreateBillRequest request, CancellationToken cancellationToken) { var doctor = await db.Doctors.SingleOrDefaultAsync(d => d.UserId == doctorUserId, cancellationToken) ?? throw new NotFoundException("Doctor profile not found."); var appointment = await db.Appointments.SingleOrDefaultAsync(a => a.AppointmentId == appointmentId && a.DoctorId == doctor.DoctorId, cancellationToken) ?? throw new NotFoundException("Appointment not found."); if (!AppointmentWorkflowRules.CanGenerateBill(appointment.Status)) throw new ConflictException("A bill can be generated only for a completed appointment."); if (request.DueDate < DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime)) throw new ConflictException("A bill due date cannot be in the past."); if (await db.Bills.AnyAsync(b => b.AppointmentId == appointmentId, cancellationToken)) throw new ConflictException("A bill already exists for this appointment."); await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken); var bill = new Bill { AppointmentId = appointmentId, PatientId = appointment.PatientId, GeneratedByDoctorId = doctor.DoctorId, Amount = request.Amount, Description = request.Description?.Trim(), DueDate = request.DueDate, Status = "Pending", GeneratedAt = clock.GetUtcNow().UtcDateTime }; db.Bills.Add(bill); try { await db.SaveChangesAsync(cancellationToken); } catch (DbUpdateException) { throw new ConflictException("A bill already exists for this appointment."); } var patientUserId = await db.Patients.Where(p => p.PatientId == appointment.PatientId).Select(p => p.UserId).SingleAsync(cancellationToken); await notifications.CreateAsync(patientUserId, "BillGenerated", $"A bill of {bill.Amount:0.00} was generated for your appointment.", cancellationToken); await transaction.CommitAsync(cancellationToken); return ToDto(bill); }
    public async Task<BillDto> GetAsync(int patientUserId, int billId, CancellationToken cancellationToken) { var patient = await db.Patients.SingleOrDefaultAsync(p => p.UserId == patientUserId, cancellationToken) ?? throw new NotFoundException("Patient profile not found."); return ToDto(await db.Bills.AsNoTracking().SingleOrDefaultAsync(b => b.BillId == billId && b.PatientId == patient.PatientId, cancellationToken) ?? throw new NotFoundException("Bill not found.")); }
    public async Task<IReadOnlyList<BillDto>> GetMineAsync(int patientUserId, PaginationRequest pagination, CancellationToken cancellationToken) { var patient = await db.Patients.SingleOrDefaultAsync(p => p.UserId == patientUserId, cancellationToken) ?? throw new NotFoundException("Patient profile not found."); return (await db.Bills.AsNoTracking().Where(b => b.PatientId == patient.PatientId).OrderByDescending(b => b.GeneratedAt).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken)).Select(ToDto).ToList(); }
    private static BillDto ToDto(Bill b) => new(b.BillId, b.AppointmentId, b.PatientId, b.GeneratedByDoctorId, b.Amount, b.Status, b.Description, b.GeneratedAt, b.DueDate);
}
