namespace Hospital.Application.DTOs.Auth;

public sealed record AuthenticationResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    AuthenticatedUserDto User);
