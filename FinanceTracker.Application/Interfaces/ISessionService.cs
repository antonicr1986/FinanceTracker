namespace FinanceTracker.Application.Interfaces;

/// <summary>
/// Revocacion de sesiones. Un JWT es valido hasta que caduca y la API no
/// guarda los que emite, asi que para invalidarlos antes se guarda en el
/// usuario desde cuando dejan de valer y se compara con la fecha de emision
/// ("iat") del token en cada peticion.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// true si la sesion de un token emitido en <paramref name="issuedAt"/>
    /// sigue valida: el usuario existe y no ha cerrado todas sus sesiones
    /// despues.
    /// </summary>
    Task<bool> IsActiveAsync(int userId, DateTime issuedAt);

    /// <summary>Invalida todos los JWT emitidos hasta ahora para el usuario.</summary>
    Task RevokeAllAsync(int userId);
}
