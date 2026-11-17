namespace FinanceTracker.Application.DTOs.Mfa;

public class MfaRecoveryCodesDto
{
    /// <summary>Se muestran una sola vez: la API solo guarda su hash.</summary>
    public List<string> RecoveryCodes { get; set; } = new();
}
