namespace FinanceTracker.Domain.Entities;

/// <summary>
/// Dispositivo en el que el usuario marco "no volver a pedir el codigo" del
/// MFA. Solo se guarda el hash del token que tiene el dispositivo, y caduca
/// pase lo que pase: despues se vuelve a pedir el codigo una vez.
/// </summary>
public class TrustedDevice
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    /// <summary>SHA-256 en hexadecimal del token.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>Hasta cuando (UTC) vale; despues el login vuelve a pedir el codigo.</summary>
    public DateTime ExpiresAt { get; set; }
}
