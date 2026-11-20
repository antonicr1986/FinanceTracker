using FinanceTracker.Application.Common;

namespace FinanceTracker.Application.Interfaces;

/// <summary>
/// Verificacion del email con un codigo de 6 digitos. Hasta verificarlo, el
/// login no entrega sesion: asi nadie puede registrarse con un email ajeno y
/// quedarse esperando a que el dueño use la cuenta.
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    /// true si el email esta verificado, o si la verificacion esta desactivada
    /// (Email:RequireVerification = false): entonces el login no la pide.
    /// </summary>
    Task<bool> IsVerifiedAsync(int userId);

    /// <summary>
    /// Tras registrarse. Con la verificacion desactivada, la cuenta queda
    /// verificada como antes de existir esto: asi, al activarla, nadie puede
    /// "sustituir" cuentas creadas mientras estaba apagada.
    /// </summary>
    Task OnRegisteredAsync(int userId);

    /// <summary>Envia un codigo si no hay ninguno vigente (no invalida el que el usuario ya tiene en el correo).</summary>
    Task EnsureCodeSentAsync(int userId);

    /// <summary>Envia un codigo nuevo, salvo que el anterior se enviara hace muy poco.</summary>
    Task<EmailVerificationOutcome> ResendAsync(int userId);

    Task<EmailVerificationOutcome> VerifyAsync(int userId, string code);
}
