using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Application.DTOs.Users;

/// <summary>Cuerpo de POST /api/users/password/forgot.</summary>
public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;
}

/// <summary>Cuerpo de POST /api/users/password/reset.</summary>
public class ResetPasswordDto
{
    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{6}$")]
    public string Code { get; set; } = string.Empty;

    // Mismas reglas que en el registro.
    [Required]
    [MinLength(12)]
    public string NewPassword { get; set; } = string.Empty;
}
