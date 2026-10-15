using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterUserDto registerUserDto)
    {
        var user = await _userService.RegisterAsync(registerUserDto);

        if (user is null)
        {
            return ApiProblems.BadRequest(
                "email_already_exists",
                "A user with this email already exists.");
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            user);
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
}
