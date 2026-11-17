namespace FinanceTracker.Application.DTOs.Mfa;

/// <summary>
/// Respuesta del login cuando la contraseña es correcta pero la cuenta tiene
/// MFA: falta el codigo. No trae JWT, solo un token temporal para
/// POST /api/users/mfa/verify.
/// </summary>
public class MfaChallengeDto
{
    public bool MfaRequired { get; set; } = true;

    public string MfaToken { get; set; } = string.Empty;

    public DateTime Expiration { get; set; }
}
