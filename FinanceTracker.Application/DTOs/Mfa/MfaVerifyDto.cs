using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Application.DTOs.Mfa;

public class MfaVerifyDto
{
    /// <summary>El MfaToken que devolvio el login.</summary>
    [Required]
    public string MfaToken { get; set; } = string.Empty;

    /// <summary>Los 6 digitos de la app autenticadora o un codigo de recuperacion.</summary>
    [Required]
    [MaxLength(32)]
    public string Code { get; set; } = string.Empty;

    /// <summary>"No volver a pedir el codigo en este dispositivo durante 30 dias".</summary>
    public bool RememberDevice { get; set; }
}
