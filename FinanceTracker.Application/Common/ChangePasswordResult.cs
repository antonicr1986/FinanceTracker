using FinanceTracker.Application.DTOs.Users;

namespace FinanceTracker.Application.Common;

public enum ChangePasswordOutcome
{
    Changed,

    /// <summary>Cuenta sin contraseña (solo Google) o la cuenta de demostracion.</summary>
    NotAvailable,

    /// <summary>La contraseña actual no es correcta, o la cuenta esta bloqueada.</summary>
    InvalidCurrentPassword,

    /// <summary>La nueva es igual que la actual.</summary>
    Unchanged,

    PasswordBreached
}

public class ChangePasswordResult
{
    public ChangePasswordOutcome Outcome { get; init; }

    /// <summary>Solo con Outcome = Changed.</summary>
    public UserDto? User { get; init; }
}
