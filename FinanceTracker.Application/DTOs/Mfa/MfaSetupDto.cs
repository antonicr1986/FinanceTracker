namespace FinanceTracker.Application.DTOs.Mfa;

public class MfaSetupDto
{
    /// <summary>Secreto en base32, para escribirlo a mano si no se puede escanear el QR.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Enlace otpauth:// que el cliente convierte en QR.</summary>
    public string OtpAuthUri { get; set; } = string.Empty;
}
