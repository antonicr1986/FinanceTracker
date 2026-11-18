using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Application.DTOs.Users;

public class LoginUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Token de dispositivo de confianza, si este dispositivo marco "no volver
    /// a pedir el codigo". Con uno valido, una cuenta con MFA entra sin codigo.
    /// </summary>
    [MaxLength(128)]
    public string? TrustedDeviceToken { get; set; }
}
