using FinanceTracker.Application.DTOs.Users;

namespace FinanceTracker.Application.Common;

public enum RegisterUserOutcome
{
    Created,
    EmailAlreadyExists,
    PasswordBreached
}

public class RegisterUserResult
{
    public RegisterUserOutcome Outcome { get; init; }

    /// <summary>Solo con Outcome = Created.</summary>
    public UserDto? User { get; init; }
}
