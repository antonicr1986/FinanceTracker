namespace FinanceTracker.Application.Interfaces;

/// <summary>Envia los correos de la aplicacion. Hoy, solo el codigo de verificacion.</summary>
public interface IEmailSender
{
    Task SendVerificationCodeAsync(string email, string code);
}
