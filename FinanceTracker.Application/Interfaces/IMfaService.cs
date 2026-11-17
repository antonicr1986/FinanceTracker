using FinanceTracker.Application.Common;
using FinanceTracker.Application.DTOs.Mfa;

namespace FinanceTracker.Application.Interfaces;

public interface IMfaService
{
    /// <summary>Genera un secreto nuevo (pendiente de confirmar).</summary>
    Task<MfaResult<MfaSetupDto>> SetupAsync(int userId);

    /// <summary>Activa el MFA si el codigo es valido y devuelve los codigos de recuperacion.</summary>
    Task<MfaResult<MfaRecoveryCodesDto>> ConfirmAsync(int userId, string code);

    /// <summary>Desactiva el MFA si el codigo es valido.</summary>
    Task<MfaOutcome> DisableAsync(int userId, string code);
}
