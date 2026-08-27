using Hospital.Application.DTOs.Auth;

namespace Hospital.Application.Interfaces;

public interface IJwtTokenService
{
    AuthenticationResponse CreateToken(AuthenticatedUserDto user);
}
