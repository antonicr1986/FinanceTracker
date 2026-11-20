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

    /// <summary>
    /// Momento (UTC, en segundos enteros) del ultimo "cerrar sesion en todos
    /// los dispositivos". Los JWT emitidos antes dejan de valer aunque no hayan
    /// caducado; null si nunca se ha hecho.
    /// </summary>
    public DateTime? SessionsRevokedAt { get; set; }

    /// <summary>
    /// Si se ha demostrado que el email es de quien tiene la cuenta: con el
    /// codigo que se envia al registrarse, o porque Google ya lo verifico.
    /// Sin verificar no se entrega sesion.
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>Hash del codigo de verificacion enviado por email; null si no hay ninguno pendiente.</summary>
    public string? EmailCodeHash { get; set; }

    public DateTime? EmailCodeExpiresAt { get; set; }

    /// <summary>Cuando se envio el ultimo codigo: limita los reenvios.</summary>
    public DateTime? EmailCodeSentAt { get; set; }

    /// <summary>Codigos incorrectos para el codigo actual. Al llegar al maximo deja de valer.</summary>
    public int EmailCodeAttempts { get; set; }

    /// <summary>
    /// Hash del codigo para recuperar la contraseña; null si no hay ninguno
    /// pendiente. Separado del de verificacion: un codigo no vale para lo otro.
    /// </summary>
    public string? PasswordResetCodeHash { get; set; }

    public DateTime? PasswordResetExpiresAt { get; set; }

    /// <summary>Cuando se envio el ultimo codigo de recuperacion: limita los reenvios.</summary>
    public DateTime? PasswordResetSentAt { get; set; }

    public int PasswordResetAttempts { get; set; }

    public List<MfaRecoveryCode> MfaRecoveryCodes { get; set; } = new();

    public List<Category> Categories { get; set; } = new();

    public List<Transaction> Transactions { get; set; } = new();

    public List<Budget> Budgets { get; set; } = new();
}
