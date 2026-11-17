namespace FinanceTracker.Application.DTOs.Users;

public class UserDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Si la cuenta tiene el MFA activo: el login no da el JWT hasta verificar
    /// el codigo, y los clientes lo muestran en ajustes.
    /// </summary>
    public bool MfaEnabled { get; set; }
}
