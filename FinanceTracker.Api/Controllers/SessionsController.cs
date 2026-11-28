using FinanceTracker.Application.Interfaces;
using FinanceTracker.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

/// <summary>
/// Sesiones de la cuenta con sesion iniciada. Por ahora solo "cerrar sesion en
/// todos los dispositivos": util si se pierde un equipo o se sospecha que
/// alguien ha robado un token.
/// </summary>
[Authorize]
[Route("api/users/sessions")]
[ApiController]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ICurrentUserService _currentUserService;

    public SessionsController(ISessionService sessionService, ICurrentUserService currentUserService)
    {
        _sessionService = sessionService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Invalida todos los JWT de la cuenta, incluido el de esta peticion: el
    /// cliente debe borrar su sesion y volver al login. No toca los
    /// dispositivos de confianza del MFA (eso es /api/users/mfa/forget-devices).
    /// </summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeAll()
    {
        if (_currentUserService.UserId is not int userId)
        {
            return Unauthorized();
        }

        if (!await _sessionService.RevokeAllAsync(userId))
        {
            return ApiProblems.Of(StatusCodes.Status403Forbidden, "demo_account",
                "The demo account is shared by every visitor: its sessions cannot be closed.");
        }

        return NoContent();
    }
}
