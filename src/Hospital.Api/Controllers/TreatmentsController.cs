using System.Security.Claims;
using Hospital.Application.DTOs.Treatments;
using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class TreatmentsController(ITreatmentService treatmentService) : ControllerBase
{
    [Authorize(Roles = UserRoles.Doctor), HttpPost("appointments/{appointmentId:int}/treatment")]
    public async Task<ActionResult<TreatmentDto>> Create(int appointmentId, CreateTreatmentRequest request, CancellationToken cancellationToken) => Ok(await treatmentService.CreateAsync(UserId, appointmentId, request, cancellationToken));
    [HttpGet("patients/{patientId:int}/history")]
    [HttpGet("patients/{patientId:int}/treatments")]
    public async Task<ActionResult<IReadOnlyList<TreatmentDto>>> History(int patientId, [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken) => Ok(await treatmentService.GetPatientHistoryAsync(UserId, Role, patientId, pagination, cancellationToken));
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
    private string Role => User.FindFirstValue(ClaimTypes.Role)!;
}
