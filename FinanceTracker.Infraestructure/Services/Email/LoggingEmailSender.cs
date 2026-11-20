using FinanceTracker.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Infrastructure.Services.Email;

/// <summary>
/// Sin servicio de correo configurado (desarrollo, tests, Docker en local,
/// AWS bajo demanda): el codigo se escribe en el log para poder seguir el
/// registro. Solo en desarrollo: en cualquier otro entorno un codigo en los
/// logs dejaria verificar cuentas a quien pueda leerlos.
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;
    private readonly bool _logCode;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger, bool logCode)
    {
        _logger = logger;
        _logCode = logCode;
    }

    public Task SendVerificationCodeAsync(string email, string code) => Log("Verification", email, code);

    public Task SendPasswordResetCodeAsync(string email, string code) => Log("Password reset", email, code);

    private Task Log(string kind, string email, string code)
    {
        if (_logCode)
        {
            _logger.LogInformation("Email not configured. {Kind} code for {Email}: {Code}", kind, email, code);
        }
        else
        {
            _logger.LogWarning("Email not configured: the {Kind} code could not be sent.", kind);
        }

        return Task.CompletedTask;
    }
}
