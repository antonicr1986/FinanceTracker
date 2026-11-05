using FinanceTracker.Application.DTOs.Users;

namespace FinanceTracker.Application.Interfaces;

public interface IGoogleTokenValidator
{
    /// <summary>
    /// Valida el ID token contra Google (firma, caducidad y audiencia).
    /// Devuelve null si el token no es valido.
    /// </summary>
    Task<GoogleIdentity?> ValidateAsync(string idToken);
}
