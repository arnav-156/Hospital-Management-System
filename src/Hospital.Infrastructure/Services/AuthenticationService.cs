using Hospital.Application.DTOs.Auth;
using Hospital.Application.Exceptions;
using Hospital.Application.Interfaces;
using Hospital.Application.Security;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public sealed class AuthenticationService(
    HospitalManagementDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService,
    TimeProvider timeProvider) : IAuthenticationService
{
    public async Task<AuthenticationResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw new ConflictException("An account with this email address already exists.");
        }

        var user = new User
        {
            Email = email,
            Role = UserRoles.Patient,
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("An account with this email address already exists.");
        }

        return jwtTokenService.CreateToken(ToDto(user));
    }

    public async Task<AuthenticationResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);
        if (user is null || !user.IsActive ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            throw new AuthenticationException();
        }

        return jwtTokenService.CreateToken(ToDto(user));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static AuthenticatedUserDto ToDto(User user) => new(user.UserId, user.Email, user.Role);
}
