using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Feedback;

public sealed class CreateFeedbackRequest { [Range(1, int.MaxValue)] public int AppointmentId { get; init; } [Range(1, 5)] public byte Rating { get; init; } [StringLength(2000)] public string? Comments { get; init; } }
