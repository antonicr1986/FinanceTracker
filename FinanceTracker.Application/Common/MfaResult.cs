namespace FinanceTracker.Application.Common;

public enum MfaOutcome
{
    Success,

    /// <summary>Cuenta sin contraseña (solo Google) o cuenta demo.</summary>
    NotAvailable,

    AlreadyEnabled,

    NotEnabled,

    /// <summary>Se intenta confirmar sin haber pedido antes el secreto.</summary>
    SetupNotStarted,

    InvalidCode
}

public class MfaResult<T>
{
    public MfaOutcome Outcome { get; init; }

    /// <summary>Solo con Outcome = Success.</summary>
    public T? Value { get; init; }

    public static MfaResult<T> Ok(T value) => new() { Outcome = MfaOutcome.Success, Value = value };

    public static MfaResult<T> Fail(MfaOutcome outcome) => new() { Outcome = outcome };
}
