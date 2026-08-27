namespace Hospital.Application.DTOs.Profiles;

public sealed record UserAccountDto(int UserId, string Email, string Role, bool IsActive, DateTime CreatedAt);
