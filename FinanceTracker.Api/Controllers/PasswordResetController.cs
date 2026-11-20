using FinanceTracker.Api.Common;
using FinanceTracker.Api.Middleware;
using FinanceTracker.Application.Common;
using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FinanceTracker.Api.Controllers;

/// <summary>
/// "¿Has olvidado tu contraseña?". Sin sesion: lo que demuestra que la cuenta
/// es tuya es el codigo que llega al correo. Mismo limite de intentos que el login.
/// </summary>
[AllowAnonymous]
[Route("api/users/password")]
[ApiController]
[EnableRateLimiting(AuthRateLimiting.PolicyName)]
public class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _passwordReset;

    public PasswordResetController(IPasswordResetService passwordReset)
    {
        _passwordReset = passwordReset;
    }

    /// <summary>Siempre 204, exista o no la cuenta: no sirve para averiguar correos registrados.</summary>
    [HttpPost("forgot")]
    public async Task<IActionResult> Forgot([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        await _passwordReset.RequestAsync(forgotPasswordDto.Email);

        return NoContent();
    }

    /// <summary>Codigo correcto: contraseña cambiada y todas las sesiones cerradas. Despues, a iniciar sesion.</summary>
    [HttpPost("reset")]
    public async Task<IActionResult> Reset([FromBody] ResetPasswordDto resetPasswordDto)
    {
        var outcome = await _passwordReset.ResetAsync(
            resetPasswordDto.Email, resetPasswordDto.Code, resetPasswordDto.NewPassword);

        return outcome switch
        {
            PasswordResetOutcome.Reset => NoContent(),
            PasswordResetOutcome.InvalidCode => ApiProblems.BadRequest("invalid_reset_code", "The code is not valid."),
            PasswordResetOutcome.PasswordBreached => ApiProblems.BadRequest(
                "password_breached",
                "This password has appeared in a data breach. Please choose a different one."),
            _ => ApiProblems.BadRequest(
                "reset_code_expired", "The code has expired or has been tried too many times. Ask for a new one.")
        };
    }
}
