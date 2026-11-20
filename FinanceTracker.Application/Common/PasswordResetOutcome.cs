namespace FinanceTracker.Application.Common;

public enum PasswordResetOutcome
{
    Reset,

    /// <summary>Codigo incorrecto (o correo sin cuenta con contraseña); quedan intentos.</summary>
    InvalidCode,

    /// <summary>No hay codigo vigente (caducado, agotados los intentos o nunca enviado): hay que pedir otro.</summary>
    Expired,

    /// <summary>La contraseña nueva aparece en filtraciones. El codigo sigue valiendo para probar otra.</summary>
    PasswordBreached
}
