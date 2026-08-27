using System;
using System.Collections.Generic;

namespace Hospital.Infrastructure.Data.Entities;

public partial class Bill
{
    public int BillId { get; set; }

    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public int? GeneratedByDoctorId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime GeneratedAt { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Doctor? GeneratedByDoctor { get; set; }
}
