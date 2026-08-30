using System.Security.Claims;
using Hospital.Application.DTOs.Profiles;
using Hospital.Application.Interfaces;
using Hospital.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(IProfileService profileService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<object>> Me(CancellationToken cancellationToken) =>
        Ok(await profileService.GetCurrentProfileAsync(CurrentUserId, CurrentRole, cancellationToken));

    [Authorize(Roles = UserRoles.Patient)]
    [HttpPut("me")]
    public async Task<ActionResult<PatientProfileDto>> UpdatePatientProfile(UpdatePatientProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await profileService.UpdatePatientProfileAsync(CurrentUserId, request, cancellationToken));

    [Authorize(Roles = UserRoles.Doctor)]
    [HttpPut("me/doctor")]
    public async Task<ActionResult<DoctorProfileDto>> UpdateDoctorProfile(UpdateDoctorOwnProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await profileService.UpdateDoctorOwnProfileAsync(CurrentUserId, request, cancellationToken));

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role)!;
}
