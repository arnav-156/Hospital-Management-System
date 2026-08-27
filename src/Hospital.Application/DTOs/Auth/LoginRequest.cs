using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Auth;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Password { get; init; } = string.Empty;
}
