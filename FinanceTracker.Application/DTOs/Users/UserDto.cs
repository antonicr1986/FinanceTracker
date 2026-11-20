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

    /// <summary>
    /// Si la cuenta tiene contraseña propia que gestionar. false en las cuentas
    /// que solo entran con Google y en la de demostracion (compartida): los
    /// clientes no les ofrecen cambiar la contraseña ni activar el MFA, que la
    /// API rechazaria igualmente.
    /// </summary>
    public bool HasPassword { get; set; }
}
