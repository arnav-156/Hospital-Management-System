namespace Hospital.Application.DTOs.Catalog;

public sealed record DepartmentDto(int DepartmentId, string DepartmentCode, string Name, string? Description, bool IsActive);
