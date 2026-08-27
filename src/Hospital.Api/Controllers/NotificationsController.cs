using System.Security.Claims;
using Hospital.Application.DTOs.Notifications;
using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Hospital.Api.Controllers;
[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationService notifications) : ControllerBase
{ [HttpGet] public async Task<ActionResult<IReadOnlyList<NotificationDto>>> Get([FromQuery] PaginationRequest pagination, CancellationToken ct) => Ok(await notifications.GetAsync(UserId, pagination, ct)); [HttpPut("{notificationId:int}/read")] public async Task<ActionResult<NotificationDto>> Read(int notificationId, CancellationToken ct) => Ok(await notifications.MarkReadAsync(UserId, notificationId, ct)); private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture); }
