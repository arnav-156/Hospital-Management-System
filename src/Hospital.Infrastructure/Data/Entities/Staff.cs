using System;
using System.Collections.Generic;

namespace Hospital.Infrastructure.Data.Entities;

public partial class Staff
{
    public int StaffId { get; set; }

    public int UserId { get; set; }

    public int? DepartmentId { get; set; }

    public string EmployeeNumber { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string JobTitle { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Department? Department { get; set; }

    public virtual User User { get; set; } = null!;
}
