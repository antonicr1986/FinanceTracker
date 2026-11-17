using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Application.DTOs.Mfa;

public class MfaCodeDto
{
    /// <summary>Los 6 digitos que muestra la app autenticadora.</summary>
    [Required]
    [RegularExpression(@"^\d{6}$")]
    public string Code { get; set; } = string.Empty;
}
