using Hospital.Application.DTOs.Auth;

namespace Hospital.Application.Interfaces;

public interface IAuthenticationService
{
    Task<AuthenticationResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthenticationResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
