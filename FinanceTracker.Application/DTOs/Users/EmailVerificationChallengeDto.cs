namespace FinanceTracker.Application.DTOs.Users;

/// <summary>
/// Respuesta del login cuando la contraseña es correcta pero el email aun no
/// esta verificado. No trae JWT: solo un token temporal para
/// POST /api/users/email/verify y /resend, y el email al que se envio el codigo.
/// </summary>
public class EmailVerificationChallengeDto
{
    public bool EmailVerificationRequired { get; set; } = true;

    public string VerificationToken { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime Expiration { get; set; }
}
