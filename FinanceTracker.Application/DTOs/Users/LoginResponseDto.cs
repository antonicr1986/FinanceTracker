namespace FinanceTracker.Application.DTOs.Users;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime Expiration { get; set; }

    public UserDto User { get; set; } = null!;

    /// <summary>
    /// Solo tras verificar el MFA marcando "no volver a pedir en este
    /// dispositivo": el cliente lo guarda y lo envia en los siguientes logins.
    /// </summary>
    public string? TrustedDeviceToken { get; set; }
}
