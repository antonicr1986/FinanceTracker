using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Services;

public class UserService : IUserService
{
    // Bloqueo por cuenta: tras MaxFailedLogins contraseñas incorrectas seguidas,
    // la cuenta no admite login con contraseña durante LockoutDuration. El
    // limite por IP (AuthRateLimiting) no frena a quien reparte los intentos
    // entre muchas IPs; esto si, porque cuenta por cuenta.
    public const int MaxFailedLogins = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly IGoogleTokenValidator _googleTokenValidator;

    public UserService(AppDbContext context, IGoogleTokenValidator googleTokenValidator)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
        _googleTokenValidator = googleTokenValidator;
    }

    public async Task<UserDto?> RegisterAsync(RegisterUserDto registerUserDto)
    {
        var emailExists = await _context.Users
            .AnyAsync(user => user.Email == registerUserDto.Email);

        if (emailExists)
        {
            return null;
        }

        var user = new User
        {
            Name = registerUserDto.Name,
            Email = registerUserDto.Email
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            registerUserDto.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Con la cuenta ya creada (y por tanto con Id), se le siembran las
        // categorias de partida. Va en el mismo flujo a proposito: si fallara,
        // quedaria una cuenta inservible para dar de alta movimientos.
        // En el idioma con el que se registra, si el cliente lo indica.
        _context.Categories.AddRange(DefaultCategories.ForUser(user.Id, registerUserDto.Language));
        await _context.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Where(user => user.Id == id)
            .Select(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            })
            .FirstOrDefaultAsync();
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Where(user => user.Email == email)
            .Select(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            })
            .FirstOrDefaultAsync();
    }

    public async Task<UserDto?> LoginAsync(LoginUserDto loginUserDto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(user => user.Email == loginUserDto.Email);

        // Sin contraseña (cuentas que solo entran con Google, o cuya contraseña
        // se anulo al vincularlas a Google): no se puede entrar con contraseña.
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return null;
        }

        // Bloqueada: se responde igual que con una contraseña incorrecta, aunque
        // sea la buena. Asi quien prueba contraseñas no sabe si ha acertado.
        if (user.LockoutEnd > DateTime.UtcNow)
        {
            return null;
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            loginUserDto.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= MaxFailedLogins)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginCount = 0;
            }

            await _context.SaveChangesAsync();

            return null;
        }

        // Login correcto: se olvidan los fallos y el bloqueo ya caducado.
        if (user.FailedLoginCount > 0 || user.LockoutEnd is not null)
        {
            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();
        }

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }

    public async Task<UserDto?> GoogleLoginAsync(string idToken, string? language)
    {
        var identity = await _googleTokenValidator.ValidateAsync(idToken);

        // Token no valido, o sin email verificado: no se puede confiar en el
        // para vincular ni para crear la cuenta.
        if (identity is null || !identity.EmailVerified)
        {
            return null;
        }

        // Primero por sub: identifica la cuenta de Google aunque cambie su email.
        var linked = await _context.Users
            .FirstOrDefaultAsync(user => user.GoogleSubject == identity.Subject);

        if (linked is not null)
        {
            return ToDto(linked);
        }

        // Ya tiene cuenta con ese email.
        var existing = await _context.Users
            .FirstOrDefaultAsync(user => user.Email == identity.Email);

        if (existing is not null)
        {
            // Ya vinculada a OTRA cuenta de Google: no se reasigna. Si no, quien
            // consiguiera otra cuenta de Google con ese email se quedaria con esta.
            if (existing.GoogleSubject is not null)
            {
                return null;
            }

            // El registro con contraseña no verifica el email, asi que esa
            // contraseña pudo ponerla otra persona que se registro antes con
            // este email para esperar a que el dueño entrase y usara la cuenta
            // (robo de cuenta preparado de antemano). Google si demuestra que el
            // email es de quien inicia sesion: se vincula y se anula la
            // contraseña. Desde ahora esta cuenta entra con Google.
            existing.GoogleSubject = identity.Subject;
            existing.PasswordHash = string.Empty;
            await _context.SaveChangesAsync();

            return ToDto(existing);
        }

        var user = new User
        {
            Name = identity.Name,
            Email = identity.Email,
            GoogleSubject = identity.Subject
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Igual que en el registro: categorias de partida en el idioma del cliente.
        _context.Categories.AddRange(DefaultCategories.ForUser(user.Id, language));
        await _context.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email
    };
}
