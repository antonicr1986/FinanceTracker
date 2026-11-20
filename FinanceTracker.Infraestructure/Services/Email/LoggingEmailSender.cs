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

    public Task SendVerificationCodeAsync(string email, string code)
    {
        if (_logCode)
        {
            _logger.LogInformation("Email not configured. Verification code for {Email}: {Code}", email, code);
        }
        else
        {
            _logger.LogWarning("Email not configured: the verification code could not be sent.");
        }

        return Task.CompletedTask;
    }
}
