using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Application.DTOs.Users;

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    // Mismas reglas que en el registro.
    [Required]
    [MinLength(12)]
    public string NewPassword { get; set; } = string.Empty;
}
