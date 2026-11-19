using FinanceTracker.Application.Common;
using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Api.Common;
using FinanceTracker.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FinanceTracker.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly IMfaService _mfaService;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(
        IUserService userService,
        ITokenService tokenService,
        IMfaService mfaService,
        ICurrentUserService currentUserService)
    {
        _userService = userService;
        _tokenService = tokenService;
        _mfaService = mfaService;
        _currentUserService = currentUserService;
    }

    [HttpPost("register")]
    [EnableRateLimiting(AuthRateLimiting.PolicyName)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterUserDto registerUserDto)
    {
        var result = await _userService.RegisterAsync(registerUserDto);

        if (result.Outcome == RegisterUserOutcome.EmailAlreadyExists)
        {
            return ApiProblems.BadRequest(
                "email_already_exists",
                "A user with this email already exists.");
        }

        if (result.Outcome == RegisterUserOutcome.PasswordBreached)
        {
            return ApiProblems.BadRequest(
                "password_breached",
                "This password has appeared in a data breach. Please choose a different one.");
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.User!.Id },
            result.User);
    }

    // Solo con sesion y solo los datos propios. Sin esto, cualquiera podia
    // recorrer los ids y obtener el nombre y el email de todos los usuarios.
    // Con un id ajeno se responde 404, igual que si no existiera, para no
    // revelar que ids estan ocupados.
    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        if (_currentUserService.UserId != id)
        {
            return NotFound();
        }

        var user = await _userService.GetByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost("login")]
    [EnableRateLimiting(AuthRateLimiting.PolicyName)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginUserDto loginUserDto)
    {
        var user = await _userService.LoginAsync(loginUserDto);

        if (user is null)
        {
            return ApiProblems.Unauthorized(
                "invalid_credentials",
                "Invalid email or password.");
        }

        return await SignInAsync(user, loginUserDto.TrustedDeviceToken);
    }

    [HttpPost("google")]
    [EnableRateLimiting(AuthRateLimiting.PolicyName)]
    public async Task<ActionResult<LoginResponseDto>> GoogleLogin([FromBody] GoogleLoginDto googleLoginDto)
    {
        var user = await _userService.GoogleLoginAsync(googleLoginDto.IdToken, googleLoginDto.Language);

        if (user is null)
        {
            return ApiProblems.Unauthorized(
                "invalid_google_token",
                "Google could not verify this account.");
        }

        return await SignInAsync(user, googleLoginDto.TrustedDeviceToken);
    }

    /// <summary>
    /// Cambiar la contraseña con sesion iniciada. Cierra todas las sesiones
    /// (tambien la de esta peticion) y devuelve un JWT nuevo para que este
    /// dispositivo siga dentro. Los errores son 400 y no 401: un 401 los
    /// clientes lo entienden como sesion caducada y cerrarian la sesion.
    /// </summary>
    [Authorize]
    [HttpPost("password")]
    [EnableRateLimiting(AuthRateLimiting.PolicyName)]
    public async Task<ActionResult<LoginResponseDto>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        if (_currentUserService.UserId is not int userId)
        {
            return Unauthorized();
        }

        var result = await _userService.ChangePasswordAsync(userId, changePasswordDto);

        return result.Outcome switch
        {
            ChangePasswordOutcome.Changed => Ok(_tokenService.CreateToken(result.User!)),
            ChangePasswordOutcome.NotAvailable => ApiProblems.BadRequest(
                "password_not_available",
                "This account has no password to change."),
            ChangePasswordOutcome.InvalidCurrentPassword => ApiProblems.BadRequest(
                "invalid_current_password",
                "The current password is not correct."),
            ChangePasswordOutcome.Unchanged => ApiProblems.BadRequest(
                "password_unchanged",
                "The new password must be different from the current one."),
            _ => ApiProblems.BadRequest(
                "password_breached",
                "This password has appeared in a data breach. Please choose a different one.")
        };
    }

    /// <summary>
    /// Con MFA activo no se entrega el JWT: solo un token temporal (MfaChallengeDto)
    /// para POST /api/users/mfa/verify. Vale tambien para Google: una cuenta con
    /// contraseña y MFA que despues se vincula a Google sigue pidiendo el codigo.
    ///
    /// Excepcion: un dispositivo de confianza ("no volver a pedir en este
    /// dispositivo") entra sin el codigo. La contraseña (o Google) se ha pedido
    /// igual: solo se salta el segundo paso.
    /// </summary>
    private async Task<ActionResult<LoginResponseDto>> SignInAsync(UserDto user, string? trustedDeviceToken)
    {
        if (user.MfaEnabled && !await _mfaService.IsTrustedDeviceAsync(user.Id, trustedDeviceToken))
        {
            return Ok(_tokenService.CreateMfaChallenge(user.Id));
        }

        return Ok(_tokenService.CreateToken(user));
    }
}
