using System;
using System.Collections.Generic;

namespace Hospital.Infrastructure.Data.Entities;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public int DepartmentId { get; set; }

    public DateTime AppointmentDateTime { get; set; }

    public short DurationMinutes { get; set; }

    public string Status { get; set; } = null!;

    public string? Reason { get; set; }

    public string? DoctorResponseNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<Treatment> Treatments { get; set; } = new List<Treatment>();
}
