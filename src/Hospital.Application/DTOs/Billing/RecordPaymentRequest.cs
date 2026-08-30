using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Billing;

public sealed class RecordPaymentRequest
{
    [Required, RegularExpression("^(Cash|Card|UPI|Insurance|Other)$")]
    public string PaymentMethod { get; init; } = string.Empty;

    [StringLength(100)]
    public string? ReferenceNumber { get; init; }
}
