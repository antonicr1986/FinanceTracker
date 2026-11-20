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
/// Segundo paso del login de una cuenta con el email sin verificar: el codigo
/// de 6 digitos que llega al correo. Sin sesion todavia: la "credencial" es el
/// token temporal que dio el login. Mismo limite de intentos que el login.
/// </summary>
[AllowAnonymous]
[Route("api/users/email")]
[ApiController]
[EnableRateLimiting(AuthRateLimiting.PolicyName)]
public class EmailVerificationController : ControllerBase
{
    private readonly IEmailVerificationService _emailVerification;
    private readonly ITokenService _tokenService;
    private readonly IUserService _userService;

    public EmailVerificationController(
        IEmailVerificationService emailVerification,
        ITokenService tokenService,
        IUserService userService)
    {
        _emailVerification = emailVerification;
        _tokenService = tokenService;
        _userService = userService;
    }

    /// <summary>Codigo correcto: email verificado y sesion iniciada.</summary>
    [HttpPost("verify")]
    public async Task<ActionResult<LoginResponseDto>> Verify([FromBody] EmailVerifyDto emailVerifyDto)
    {
        var userId = _tokenService.ValidateEmailVerificationChallenge(emailVerifyDto.VerificationToken);

        if (userId is null)
        {
            return InvalidToken();
        }

        var outcome = await _emailVerification.VerifyAsync(userId.Value, emailVerifyDto.Code);

        if (outcome == EmailVerificationOutcome.InvalidCode)
        {
            return ApiProblems.BadRequest("invalid_email_code", "The code is not valid.");
        }

        if (outcome != EmailVerificationOutcome.Verified)
        {
            return ApiProblems.BadRequest(
                "email_code_expired", "The code has expired or has been tried too many times. Ask for a new one.");
        }

        var user = await _userService.GetByIdAsync(userId.Value);

        if (user is null)
        {
            return InvalidToken();
        }

        // Una cuenta nueva no tiene MFA; por si acaso, el mismo criterio que el login.
        return user.MfaEnabled
            ? Ok(_tokenService.CreateMfaChallenge(user.Id))
            : Ok(_tokenService.CreateToken(user));
    }

    /// <summary>Envia un codigo nuevo (como mucho uno por minuto).</summary>
    [HttpPost("resend")]
    public async Task<IActionResult> Resend([FromBody] EmailResendDto emailResendDto)
    {
        var userId = _tokenService.ValidateEmailVerificationChallenge(emailResendDto.VerificationToken);

        if (userId is null)
        {
            return InvalidToken();
        }

        var outcome = await _emailVerification.ResendAsync(userId.Value);

        return outcome == EmailVerificationOutcome.RecentlySent
            ? ApiProblems.BadRequest("email_code_recently_sent", "A code was sent less than a minute ago.")
            : NoContent();
    }

    private ObjectResult InvalidToken() =>
        ApiProblems.Unauthorized(
            "invalid_verification_token", "The verification step has expired. Please sign in again.");
}
