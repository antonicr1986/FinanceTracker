using FinanceTracker.Application.Common;

namespace FinanceTracker.Application.Interfaces;

/// <summary>"¿Has olvidado tu contraseña?": codigo de 6 digitos por correo y contraseña nueva.</summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Envia un codigo si el correo es de una cuenta con contraseña propia. No
    /// dice si lo ha enviado: la respuesta es la misma exista o no la cuenta.
    /// </summary>
    Task RequestAsync(string email);

    /// <summary>
    /// Con el codigo correcto cambia la contraseña, da el correo por verificado,
    /// quita el bloqueo por intentos y cierra todas las sesiones.
    /// </summary>
    Task<PasswordResetOutcome> ResetAsync(string email, string code, string newPassword);
}
