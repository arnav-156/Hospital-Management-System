using Hospital.Application.DTOs.Catalog;
using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/doctors")]
public sealed class DoctorsController(ICatalogService catalogService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DoctorSummaryDto>>> GetAll([FromQuery] int? departmentId, [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken) => Ok(await catalogService.GetDoctorsAsync(departmentId, pagination, cancellationToken));

    [AllowAnonymous]
    [HttpGet("{doctorId:int}")]
    public async Task<ActionResult<DoctorSummaryDto>> GetById(int doctorId, CancellationToken cancellationToken) => Ok(await catalogService.GetDoctorAsync(doctorId, cancellationToken));
}
