using System;
using System.Collections.Generic;

namespace Hospital.Infrastructure.Data.Entities;

public partial class Treatment
{
    public int TreatmentId { get; set; }

    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public string? Diagnosis { get; set; }

    public string? Prescription { get; set; }

    public string? ProgressNotes { get; set; }

    public string? TreatmentNotes { get; set; }

    public DateTime TreatmentDateTime { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;
}
