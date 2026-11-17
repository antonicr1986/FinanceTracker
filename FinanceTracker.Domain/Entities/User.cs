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

    public List<Category> Categories { get; set; } = new();

    public List<Transaction> Transactions { get; set; } = new();

    public List<Budget> Budgets { get; set; } = new();
}
