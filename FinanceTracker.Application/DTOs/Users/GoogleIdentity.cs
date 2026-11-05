namespace FinanceTracker.Application.DTOs.Users;

/// <summary>
/// Datos de la persona que Google garantiza tras validar su ID token.
/// Subject es el "sub" de Google: el identificador permanente de la cuenta,
/// que no cambia aunque cambie el email.
/// </summary>
public record GoogleIdentity(string Subject, string Email, bool EmailVerified, string Name);
