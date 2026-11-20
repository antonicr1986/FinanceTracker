namespace FinanceTracker.Application.Interfaces;

/// <summary>Envia los correos de la aplicacion: los codigos de verificacion y de recuperacion.</summary>
public interface IEmailSender
{
    Task SendVerificationCodeAsync(string email, string code);

    Task SendPasswordResetCodeAsync(string email, string code);
}
