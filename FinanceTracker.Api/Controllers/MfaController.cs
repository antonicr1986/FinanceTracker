using FinanceTracker.Api.Common;
using FinanceTracker.Api.Middleware;
using FinanceTracker.Application.Common;
using FinanceTracker.Application.DTOs.Mfa;
using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FinanceTracker.Api.Controllers;

/// <summary>
/// Activar y desactivar el MFA de la cuenta con sesion iniciada, y el segundo
/// paso del login (verify). El limite de intentos es el mismo que el del
/// login: frena probar codigos a ciegas.
/// </summary>
[Authorize]
[Route("api/users/mfa")]
[ApiController]
[EnableRateLimiting(AuthRateLimiting.PolicyName)]
public class MfaController : ControllerBase
{
    private readonly IMfaService _mfaService;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;

    public MfaController(IMfaService mfaService, ITokenService tokenService, ICurrentUserService currentUserService)
    {
        _mfaService = mfaService;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
    }

    // Sin sesion todavia: la "credencial" es el MfaToken que dio el login.
    [AllowAnonymous]
    [HttpPost("verify")]
    public async Task<ActionResult<LoginResponseDto>> Verify([FromBody] MfaVerifyDto mfaVerifyDto)
    {
        var userId = _tokenService.ValidateMfaChallenge(mfaVerifyDto.MfaToken);

        if (userId is null)
        {
            return ApiProblems.Unauthorized(
                "invalid_mfa_token", "The sign-in step has expired. Please sign in again.");
        }

        var result = await _mfaService.VerifyLoginAsync(userId.Value, mfaVerifyDto.Code);

        if (result.Outcome != MfaOutcome.Success)
        {
            return ApiProblems.Unauthorized("invalid_mfa_code", "The code is not valid.");
        }

        return Ok(_tokenService.CreateToken(result.Value!));
    }

    [HttpPost("setup")]
    public async Task<ActionResult<MfaSetupDto>> Setup()
    {
        var result = await _mfaService.SetupAsync(CurrentUserId);

        return result.Outcome == MfaOutcome.Success ? Ok(result.Value) : ToProblem(result.Outcome);
    }

    [HttpPost("confirm")]
    public async Task<ActionResult<MfaRecoveryCodesDto>> Confirm([FromBody] MfaCodeDto mfaCodeDto)
    {
        var result = await _mfaService.ConfirmAsync(CurrentUserId, mfaCodeDto.Code);

        return result.Outcome == MfaOutcome.Success ? Ok(result.Value) : ToProblem(result.Outcome);
    }

    [HttpPost("disable")]
    public async Task<IActionResult> Disable([FromBody] MfaCodeDto mfaCodeDto)
    {
        var outcome = await _mfaService.DisableAsync(CurrentUserId, mfaCodeDto.Code);

        return outcome == MfaOutcome.Success ? NoContent() : ToProblem(outcome);
    }

    // [Authorize] garantiza que hay usuario; si faltara el claim, es un fallo nuestro.
    private int CurrentUserId => _currentUserService.UserId
        ?? throw new InvalidOperationException("Authenticated request without user id.");

    private static ObjectResult ToProblem(MfaOutcome outcome) => outcome switch
    {
        MfaOutcome.NotAvailable => ApiProblems.BadRequest(
            "mfa_not_available", "Two-factor authentication is not available for this account."),
        MfaOutcome.AlreadyEnabled => ApiProblems.BadRequest(
            "mfa_already_enabled", "Two-factor authentication is already enabled."),
        MfaOutcome.NotEnabled => ApiProblems.BadRequest(
            "mfa_not_enabled", "Two-factor authentication is not enabled."),
        MfaOutcome.SetupNotStarted => ApiProblems.BadRequest(
            "mfa_setup_not_started", "Start the two-factor setup before confirming it."),
        _ => ApiProblems.BadRequest(
            "invalid_mfa_code", "The code is not valid.")
    };
}
