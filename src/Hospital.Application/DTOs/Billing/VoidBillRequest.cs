using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Billing;

public sealed class VoidBillRequest
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Reason { get; init; } = string.Empty;
}
