using FinanceTracker.Application.Common;
using FinanceTracker.Application.DTOs.Mfa;
using FinanceTracker.Application.DTOs.Users;

namespace FinanceTracker.Application.Interfaces;

public interface IMfaService
{
    /// <summary>Genera un secreto nuevo (pendiente de confirmar).</summary>
    Task<MfaResult<MfaSetupDto>> SetupAsync(int userId);

    /// <summary>Activa el MFA si el codigo es valido y devuelve los codigos de recuperacion.</summary>
    Task<MfaResult<MfaRecoveryCodesDto>> ConfirmAsync(int userId, string code);

    /// <summary>Desactiva el MFA si el codigo es valido.</summary>
    Task<MfaOutcome> DisableAsync(int userId, string code);

    /// <summary>
    /// Segundo paso del login: codigo TOTP o de recuperacion. Los fallos
    /// cuentan para el bloqueo de la cuenta, igual que las contraseñas.
    /// </summary>
    Task<MfaResult<UserDto>> VerifyLoginAsync(int userId, string code);

    /// <summary>Crea un dispositivo de confianza y devuelve su token (solo se guarda el hash).</summary>
    Task<string> TrustDeviceAsync(int userId);

    /// <summary>true si el token es de un dispositivo de confianza de este usuario y no ha caducado.</summary>
    Task<bool> IsTrustedDeviceAsync(int userId, string? deviceToken);

    /// <summary>"Olvidar todos los dispositivos": el siguiente login pide el codigo en todos.</summary>
    Task ForgetDevicesAsync(int userId);
}
