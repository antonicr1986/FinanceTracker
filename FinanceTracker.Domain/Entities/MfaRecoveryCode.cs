namespace FinanceTracker.Domain.Entities;

/// <summary>
/// Codigo de recuperacion del MFA, para entrar si se pierde el movil. De un
/// solo uso. Se guarda solo su hash: el codigo se muestra una unica vez.
/// </summary>
public class MfaRecoveryCode
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public string CodeHash { get; set; } = string.Empty;

    /// <summary>Cuando se uso (UTC); null si sigue disponible.</summary>
    public DateTime? UsedAt { get; set; }
}
