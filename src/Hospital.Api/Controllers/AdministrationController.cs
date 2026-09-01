using System.Security.Claims;
using Hospital.Application.DTOs;
using Hospital.Application.DTOs.Catalog;
using Hospital.Application.DTOs.Profiles;
using Hospital.Application.Interfaces;
using Hospital.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Administrator)]
[Route("api/admin")]
public sealed class AdministrationController(IProfileService profileService, ICatalogService catalogService) : ControllerBase
{
    [HttpGet("patients")]
    public async Task<ActionResult<IReadOnlyList<PatientProfileDto>>> GetPatients([FromQuery] string? search, [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken) =>
        Ok(await profileService.GetPatientsAsync(search, pagination, cancellationToken));

    [HttpGet("doctors")]
    public async Task<ActionResult<IReadOnlyList<DoctorProfileDto>>> GetDoctors([FromQuery] string? search, [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken) =>
        Ok(await profileService.GetDoctorsAsync(search, pagination, cancellationToken));

    [HttpPost("doctors")]
    public async Task<ActionResult<DoctorProfileDto>> CreateDoctor(CreateDoctorProfileRequest request, CancellationToken cancellationToken)
    {
        var doctor = await profileService.CreateDoctorAsync(request, cancellationToken);
        return Created($"/api/admin/doctors/{doctor.DoctorId}", doctor);
    }

    [HttpGet("staff")]
    public async Task<ActionResult<IReadOnlyList<StaffProfileDto>>> GetStaff([FromQuery] string? search, [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken) =>
        Ok(await profileService.GetStaffAsync(search, pagination, cancellationToken));

    [HttpGet("departments")]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> GetDepartments([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken) =>
        Ok(await catalogService.GetAllDepartmentsAsync(pagination, cancellationToken));

    [HttpPut("patients/{patientId:int}")]
    public async Task<ActionResult<PatientProfileDto>> UpdatePatient(int patientId, UpdatePatientProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await profileService.UpdatePatientAsync(patientId, request, cancellationToken));

    [HttpPut("doctors/{doctorId:int}")]
    public async Task<ActionResult<DoctorProfileDto>> UpdateDoctor(int doctorId, UpdateDoctorProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await profileService.UpdateDoctorAsync(doctorId, request, cancellationToken));

    [HttpPut("staff/{staffId:int}")]
    public async Task<ActionResult<StaffProfileDto>> UpdateStaff(int staffId, UpdateStaffProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await profileService.UpdateStaffAsync(staffId, request, cancellationToken));

    [HttpPatch("accounts/{userId:int}/status")]
    public async Task<ActionResult<UserAccountDto>> UpdateAccountStatus(int userId, UpdateAccountStatusRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
        if (currentUserId == userId)
        {
            return BadRequest(new ProblemDetails { Title = "Administrators cannot change their own account status." });
        }

        return Ok(await profileService.UpdateAccountStatusAsync(userId, request.IsActive, cancellationToken));
    }
}
