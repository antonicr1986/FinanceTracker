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
    private readonly ICurrentUserService _currentUserService;

    public UsersController(
        IUserService userService,
        ITokenService tokenService,
        ICurrentUserService currentUserService)
    {
        _userService = userService;
        _tokenService = tokenService;
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

        var response = _tokenService.CreateToken(user);

        return Ok(response);
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

        var response = _tokenService.CreateToken(user);

        return Ok(response);
    }
}
