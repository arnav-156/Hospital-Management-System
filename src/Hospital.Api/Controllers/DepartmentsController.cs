using Hospital.Application.DTOs.Catalog;
using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/departments")]
public sealed class DepartmentsController(ICatalogService catalogService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> GetAll([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken) => Ok(await catalogService.GetDepartmentsAsync(pagination, cancellationToken));

    [AllowAnonymous]
    [HttpGet("{departmentId:int}")]
    public async Task<ActionResult<DepartmentDto>> GetById(int departmentId, CancellationToken cancellationToken) => Ok(await catalogService.GetDepartmentAsync(departmentId, cancellationToken));

    [Authorize(Roles = UserRoles.Administrator)]
    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create(SaveDepartmentRequest request, CancellationToken cancellationToken)
    {
        var department = await catalogService.CreateDepartmentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { departmentId = department.DepartmentId }, department);
    }

    [Authorize(Roles = UserRoles.Administrator)]
    [HttpPut("{departmentId:int}")]
    public async Task<ActionResult<DepartmentDto>> Update(int departmentId, SaveDepartmentRequest request, CancellationToken cancellationToken) => Ok(await catalogService.UpdateDepartmentAsync(departmentId, request, cancellationToken));

    [AllowAnonymous]
    [HttpGet("{departmentId:int}/doctors")]
    public async Task<ActionResult<IReadOnlyList<DoctorSummaryDto>>> GetDoctors(int departmentId, [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken) => Ok(await catalogService.GetDoctorsAsync(departmentId, pagination, cancellationToken));
}
