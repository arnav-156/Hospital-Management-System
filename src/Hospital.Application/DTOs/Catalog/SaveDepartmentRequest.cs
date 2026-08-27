using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Catalog;

public sealed class SaveDepartmentRequest
{
    [Required, StringLength(20)] public string DepartmentCode { get; init; } = string.Empty;
    [Required, StringLength(100)] public string Name { get; init; } = string.Empty;
    [StringLength(500)] public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}
