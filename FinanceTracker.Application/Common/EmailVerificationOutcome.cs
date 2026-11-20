namespace FinanceTracker.Application.Common;

public enum EmailVerificationOutcome
{
    Verified,

    /// <summary>Codigo incorrecto; quedan intentos.</summary>
    InvalidCode,

    /// <summary>No hay codigo vigente (caducado, agotados los intentos o nunca enviado): hay que pedir otro.</summary>
    Expired,

    /// <summary>Reenvio pedido antes de tiempo.</summary>
    RecentlySent,

    Sent
}
