namespace Hospital.Infrastructure.Data.Entities;

public sealed class AiSummaryAudit
{
    public long AiSummaryAuditId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime RequestedAt { get; set; }
    public string Outcome { get; set; } = null!;
    public string? Model { get; set; }
    public int RecordCount { get; set; }
    public string? FailureCode { get; set; }
}
