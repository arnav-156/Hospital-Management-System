using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Billing;

public sealed class CreateBillRequest { [Range(typeof(decimal), "0.01", "9999999999.99")] public decimal Amount { get; init; } [StringLength(1000)] public string? Description { get; init; } public DateOnly? DueDate { get; init; } }
