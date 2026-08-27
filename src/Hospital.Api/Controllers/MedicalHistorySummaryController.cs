using System.Security.Claims;
using Hospital.Application.DTOs.Ai;
using Hospital.Application.Interfaces;
using Hospital.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Doctor)]
[Route("api/patients")]
public sealed class MedicalHistorySummaryController(IMedicalHistorySummaryService summaries) : ControllerBase
{
    [HttpPost("{patientId:int}/history-summary")]
    public async Task<ActionResult<MedicalHistorySummaryDto>> Generate(int patientId, CancellationToken cancellationToken) =>
        Ok(await summaries.GenerateAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture), patientId, cancellationToken));
}
