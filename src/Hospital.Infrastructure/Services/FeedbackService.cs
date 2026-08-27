using Hospital.Application.DTOs.Feedback;
using Hospital.Application.DTOs;
using Hospital.Application.Exceptions;
using Hospital.Application.Interfaces;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public sealed class FeedbackService(HospitalManagementDbContext db, TimeProvider clock) : IFeedbackService
{ public async Task<FeedbackDto> CreateAsync(int patientUserId, CreateFeedbackRequest request, CancellationToken cancellationToken) { var patient = await db.Patients.SingleOrDefaultAsync(p => p.UserId == patientUserId, cancellationToken) ?? throw new NotFoundException("Patient profile not found."); var appointment = await db.Appointments.SingleOrDefaultAsync(a => a.AppointmentId == request.AppointmentId && a.PatientId == patient.PatientId, cancellationToken) ?? throw new NotFoundException("Appointment not found."); if (appointment.Status != "Completed") throw new ConflictException("Feedback can be submitted only for a completed appointment."); if (await db.Feedbacks.AnyAsync(f => f.AppointmentId == appointment.AppointmentId, cancellationToken)) throw new ConflictException("Feedback already exists for this appointment."); var feedback = new Feedback { AppointmentId = appointment.AppointmentId, PatientId = patient.PatientId, Rating = request.Rating, Comments = request.Comments?.Trim(), CreatedAt = clock.GetUtcNow().UtcDateTime }; db.Feedbacks.Add(feedback); try { await db.SaveChangesAsync(cancellationToken); } catch (DbUpdateException) { throw new ConflictException("Feedback already exists for this appointment."); } return ToDto(feedback); } public async Task<IReadOnlyList<FeedbackDto>> GetMineAsync(int patientUserId, PaginationRequest pagination, CancellationToken cancellationToken) { var patient = await db.Patients.SingleOrDefaultAsync(p => p.UserId == patientUserId, cancellationToken) ?? throw new NotFoundException("Patient profile not found."); return (await db.Feedbacks.AsNoTracking().Where(f => f.PatientId == patient.PatientId).OrderByDescending(f => f.CreatedAt).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken)).Select(ToDto).ToList(); } private static FeedbackDto ToDto(Feedback f) => new(f.FeedbackId, f.AppointmentId, f.PatientId, f.Rating, f.Comments, f.CreatedAt); }
