using System.Security.Claims;
using Hospital.Application.DTOs.Billing;
using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class BillsController(IBillingService billing) : ControllerBase
{
    [Authorize(Roles = UserRoles.Doctor), HttpPost("appointments/{appointmentId:int}/bill")]
    public async Task<ActionResult<BillDto>> Create(int appointmentId, CreateBillRequest request, CancellationToken ct) => Ok(await billing.CreateAsync(UserId, appointmentId, request, ct));
    [Authorize(Roles = UserRoles.Patient), HttpGet("bills/{billId:int}")]
    public async Task<ActionResult<BillDto>> Get(int billId, CancellationToken ct) => Ok(await billing.GetAsync(UserId, billId, ct));
    [Authorize(Roles = UserRoles.Patient), HttpGet("bills/my")]
    public async Task<ActionResult<IReadOnlyList<BillDto>>> Mine([FromQuery] PaginationRequest pagination, CancellationToken ct) => Ok(await billing.GetMineAsync(UserId, pagination, ct));

    [Authorize(Roles = UserRoles.Patient), HttpPost("bills/{billId:int}/payments")]
    public async Task<ActionResult<BillDto>> RecordPayment(int billId, RecordPaymentRequest request, CancellationToken ct) => Ok(await billing.RecordPaymentAsync(UserId, billId, request, ct));

    [Authorize(Roles = UserRoles.Patient), HttpGet("bills/{billId:int}/payments")]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> PaymentHistory(int billId, CancellationToken ct) => Ok(await billing.GetPaymentHistoryAsync(UserId, billId, ct));

    [Authorize(Roles = UserRoles.Doctor), HttpGet("doctor/bills")]
    public async Task<ActionResult<IReadOnlyList<BillDto>>> DoctorBills([FromQuery] PaginationRequest pagination, CancellationToken ct) => Ok(await billing.GetDoctorBillsAsync(UserId, pagination, ct));

    [Authorize(Roles = UserRoles.Doctor), HttpGet("doctor/bills/{billId:int}/payments")]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> DoctorPaymentHistory(int billId, CancellationToken ct) => Ok(await billing.GetDoctorPaymentHistoryAsync(UserId, billId, ct));

    [Authorize(Roles = UserRoles.Doctor), HttpPut("bills/{billId:int}/void")]
    public async Task<ActionResult<BillDto>> Void(int billId, VoidBillRequest request, CancellationToken ct) => Ok(await billing.VoidAsync(UserId, billId, request, ct));

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
}
