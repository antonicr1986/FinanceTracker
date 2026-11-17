namespace FinanceTracker.Application.Interfaces;

public interface IPasswordBreachChecker
{
    /// <summary>
    /// true si la contraseña aparece en filtraciones conocidas. Si no se puede
    /// comprobar (servicio caido), devuelve false: no bloquea el registro.
    /// </summary>
    Task<bool> IsBreachedAsync(string password);
}
