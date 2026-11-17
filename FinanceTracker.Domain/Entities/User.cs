namespace FinanceTracker.Domain.Entities;

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// "sub" de Google si la cuenta se ha usado con Google; null si no.
    /// </summary>
    public string? GoogleSubject { get; set; }

    /// <summary>
    /// Contraseñas incorrectas seguidas desde el ultimo login correcto o el
    /// ultimo bloqueo.
    /// </summary>
    public int FailedLoginCount { get; set; }

    /// <summary>
    /// Hasta cuando (UTC) no se admite login con contraseña; null si no esta
    /// bloqueada.
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// Secreto TOTP en base32, cifrado con MfaSecretProtector. Existe desde que
    /// se inicia la configuracion del MFA, aunque aun no este activo.
    /// </summary>
    public string? MfaSecret { get; set; }

    /// <summary>true cuando el usuario ha confirmado su primer codigo.</summary>
    public bool MfaEnabled { get; set; }

    /// <summary>
    /// Ultimo intervalo de 30 s cuyo codigo se acepto. Un codigo de ese
    /// intervalo o anterior se rechaza: cada codigo vale una sola vez.
    /// </summary>
    public long? MfaLastUsedStep { get; set; }

    public List<MfaRecoveryCode> MfaRecoveryCodes { get; set; } = new();

    public List<Category> Categories { get; set; } = new();

    public List<Transaction> Transactions { get; set; } = new();

    public List<Budget> Budgets { get; set; } = new();
}
