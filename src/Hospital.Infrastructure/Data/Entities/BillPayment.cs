using System;

namespace Hospital.Infrastructure.Data.Entities;

public partial class BillPayment
{
    public int PaymentId { get; set; }

    public int BillId { get; set; }

    public int RecordedByPatientId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? ReferenceNumber { get; set; }

    public DateTime RecordedAt { get; set; }

    public virtual Bill Bill { get; set; } = null!;

    public virtual Patient RecordedByPatient { get; set; } = null!;
}
