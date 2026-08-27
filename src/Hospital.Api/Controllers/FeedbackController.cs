using System.Security.Claims;
using Hospital.Application.DTOs.Feedback;
using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Patient)]
[Route("api/feedback")]
public sealed class FeedbackController(IFeedbackService feedback) : ControllerBase
{ [HttpPost] public async Task<ActionResult<FeedbackDto>> Create(CreateFeedbackRequest request, CancellationToken ct) => Ok(await feedback.CreateAsync(UserId, request, ct)); [HttpGet] public async Task<ActionResult<IReadOnlyList<FeedbackDto>>> Mine([FromQuery] PaginationRequest pagination, CancellationToken ct) => Ok(await feedback.GetMineAsync(UserId, pagination, ct)); private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture); }
