using System;
using System.Collections.Generic;

namespace Hospital.Infrastructure.Data.Entities;

public partial class Doctor
{
    public int DoctorId { get; set; }

    public int UserId { get; set; }

    public int DepartmentId { get; set; }

    public string LicenseNumber { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Specialization { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public decimal ConsultationFee { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual Department Department { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
