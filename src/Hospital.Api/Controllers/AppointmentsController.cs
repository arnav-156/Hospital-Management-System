using System.Security.Claims;
using Hospital.Application.DTOs.Appointments;
using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class AppointmentsController(IAppointmentService appointments) : ControllerBase
{
    [AllowAnonymous, HttpGet("doctors/{doctorId:int}/slots")]
    public async Task<ActionResult<IReadOnlyList<DateTime>>> Slots(int doctorId, [FromQuery] DateOnly date, CancellationToken ct) => Ok(await appointments.GetAvailableSlotsAsync(doctorId, date, ct));
    [Authorize(Roles = UserRoles.Patient), HttpPost("appointments")]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentRequest request, CancellationToken ct) { var result = await appointments.CreateAsync(UserId, request, ct); return CreatedAtAction(nameof(GetById), new { appointmentId = result.AppointmentId }, result); }
    [Authorize(Roles = UserRoles.Patient), HttpGet("appointments/my")]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> Mine([FromQuery] PaginationRequest pagination, CancellationToken ct) => Ok(await appointments.GetPatientAppointmentsAsync(UserId, pagination, ct));
    [Authorize(Roles = UserRoles.Patient), HttpGet("appointments/{appointmentId:int}")]
    public async Task<ActionResult<AppointmentDto>> GetById(int appointmentId, CancellationToken ct) => Ok(await appointments.GetPatientAppointmentAsync(UserId, appointmentId, ct));
    [Authorize(Roles = UserRoles.Doctor), HttpGet("doctor/appointments/pending")]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> Pending([FromQuery] PaginationRequest pagination, CancellationToken ct) => Ok(await appointments.GetDoctorAppointmentsAsync(UserId, false, pagination, ct));
    [Authorize(Roles = UserRoles.Doctor), HttpGet("doctor/appointments/today")]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> Today([FromQuery] PaginationRequest pagination, CancellationToken ct) => Ok(await appointments.GetDoctorAppointmentsAsync(UserId, true, pagination, ct));
    [Authorize(Roles = UserRoles.Doctor), HttpGet("doctor/appointments/work-items")]
    public async Task<ActionResult<IReadOnlyList<DoctorAppointmentWorkItemDto>>> WorkItems([FromQuery] PaginationRequest pagination, CancellationToken ct) => Ok(await appointments.GetDoctorWorkItemsAsync(UserId, pagination, ct));
    [Authorize(Roles = UserRoles.Doctor), HttpPut("appointments/{appointmentId:int}/accept")]
    public async Task<ActionResult<AppointmentDto>> Accept(int appointmentId, AppointmentDecisionRequest request, CancellationToken ct) => Ok(await appointments.DecideAsync(UserId, appointmentId, true, request.Note, ct));
    [Authorize(Roles = UserRoles.Doctor), HttpPut("appointments/{appointmentId:int}/reject")]
    public async Task<ActionResult<AppointmentDto>> Reject(int appointmentId, AppointmentDecisionRequest request, CancellationToken ct) => Ok(await appointments.DecideAsync(UserId, appointmentId, false, request.Note, ct));
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
}
