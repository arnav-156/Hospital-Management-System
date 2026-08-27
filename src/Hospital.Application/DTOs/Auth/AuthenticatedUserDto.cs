namespace Hospital.Application.DTOs.Auth;

public sealed record AuthenticatedUserDto(int UserId, string Email, string Role);
