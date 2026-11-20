using Azure;
using Azure.Communication.Email;
using FinanceTracker.Application.Interfaces;

namespace FinanceTracker.Infrastructure.Services.Email;

/// <summary>
/// Envio con Azure Communication Services. El EmailClient se autentica con la
/// identidad administrada del App Service (sin claves que guardar).
/// </summary>
public class AcsEmailSender : IEmailSender
{
    private readonly EmailClient _client;
    private readonly string _senderAddress;

    public AcsEmailSender(EmailClient client, string senderAddress)
    {
        _client = client;
        _senderAddress = senderAddress;
    }

    public async Task SendVerificationCodeAsync(string email, string code)
    {
        var content = new EmailContent("Tu código de FinanceTracker / Your FinanceTracker code")
        {
            PlainText = VerificationEmail.PlainText(code),
            Html = VerificationEmail.Html(code)
        };

        // WaitUntil.Started: ACS acepta el envio y lo hace en segundo plano. No
        // hace falta esperar a la entrega para responder al usuario.
        await _client.SendAsync(WaitUntil.Started, new EmailMessage(_senderAddress, email, content));
    }
}

/// <summary>Texto del correo, en español y en inglés (el registro no guarda el idioma).</summary>
public static class VerificationEmail
{
    public static string PlainText(string code) => $"""
        Tu código para verificar tu correo en FinanceTracker es: {code}
        Caduca en 15 minutos. Si no has creado una cuenta, ignora este correo.

        Your code to verify your email in FinanceTracker is: {code}
        It expires in 15 minutes. If you did not create an account, ignore this email.
        """;

    public static string Html(string code) => $"""
        <div style="font-family:Segoe UI,Arial,sans-serif;color:#0f172a;max-width:480px">
          <p>Tu código para verificar tu correo en <b>FinanceTracker</b> es:</p>
          <p style="font-size:28px;font-weight:600;letter-spacing:6px">{code}</p>
          <p style="color:#64748b">Caduca en 15 minutos. Si no has creado una cuenta, ignora este correo.</p>
          <hr style="border:none;border-top:1px solid #e2e8f0">
          <p>Your code to verify your email in <b>FinanceTracker</b> is <b>{code}</b>.</p>
          <p style="color:#64748b">It expires in 15 minutes. If you did not create an account, ignore this email.</p>
        </div>
        """;
}
