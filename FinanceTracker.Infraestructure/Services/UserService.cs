using FinanceTracker.Application.Common;
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
    private readonly IPasswordBreachChecker _passwordBreachChecker;
    private readonly IGoogleTokenValidator _googleTokenValidator;

    public UserService(
        AppDbContext context,
        IPasswordBreachChecker passwordBreachChecker,
        IGoogleTokenValidator googleTokenValidator)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
        _passwordBreachChecker = passwordBreachChecker;
        _googleTokenValidator = googleTokenValidator;
    }

    public async Task<RegisterUserResult> RegisterAsync(RegisterUserDto registerUserDto)
    {
        var existing = await _context.Users
            .FirstOrDefaultAsync(user => user.Email == registerUserDto.Email);

        // Una cuenta con el email sin verificar (y sin Google) no es de nadie:
        // nadie ha demostrado que ese correo sea suyo. Quien se registra ahora
        // la sustituye. Si era un atacante que se adelanto con el email de otro,
        // su contraseña deja de valer; y sin acceso al correo, tampoco podra
        // verificar la nueva.
        if (existing is not null && (existing.EmailVerified || existing.GoogleSubject is not null))
        {
            return new RegisterUserResult { Outcome = RegisterUserOutcome.EmailAlreadyExists };
        }

        // Una contraseña que ya circula en filtraciones es lo primero que se
        // prueba en un ataque de diccionario, por larga que sea.
        if (await _passwordBreachChecker.IsBreachedAsync(registerUserDto.Password))
        {
            return new RegisterUserResult { Outcome = RegisterUserOutcome.PasswordBreached };
        }

        if (existing is not null)
        {
            existing.Name = registerUserDto.Name;
            existing.PasswordHash = _passwordHasher.HashPassword(existing, registerUserDto.Password);
            existing.FailedLoginCount = 0;
            existing.LockoutEnd = null;
            EmailVerificationService.ClearCode(existing);
            await _context.SaveChangesAsync();

            return new RegisterUserResult
            {
                Outcome = RegisterUserOutcome.Created,
                User = ToDto(existing)
            };
        }

        // EmailVerified = false: el login no dara sesion hasta escribir el
        // codigo que se envia al correo.
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

        return new RegisterUserResult
        {
            Outcome = RegisterUserOutcome.Created,
            User = ToDto(user)
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
                Email = user.Email,
                MfaEnabled = user.MfaEnabled,
                HasPassword = user.PasswordHash != "" && user.Email != DemoDataSeeder.DemoEmail
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
                Email = user.Email,
                MfaEnabled = user.MfaEnabled,
                HasPassword = user.PasswordHash != "" && user.Email != DemoDataSeeder.DemoEmail
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
            await RegisterFailedAttemptAsync(user);

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
            Email = user.Email,
            MfaEnabled = user.MfaEnabled,
            HasPassword = HasOwnPassword(user)
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
            // Google verifica el email: si quedaba sin verificar, ya lo esta.
            if (!linked.EmailVerified)
            {
                linked.EmailVerified = true;
                EmailVerificationService.ClearCode(linked);
                await _context.SaveChangesAsync();
            }

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
            // contraseña. Desde ahora esta cuenta entra con Google. Tambien se
            // cierran las sesiones abiertas con esa contraseña: si eran del
            // atacante, sus JWT dejan de valer en ese momento.
            existing.GoogleSubject = identity.Subject;
            existing.PasswordHash = string.Empty;
            existing.EmailVerified = true;
            EmailVerificationService.ClearCode(existing);
            SessionService.Revoke(existing);
            await _context.SaveChangesAsync();

            return ToDto(existing);
        }

        var user = new User
        {
            Name = identity.Name,
            Email = identity.Email,
            GoogleSubject = identity.Subject,
            EmailVerified = true
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
            Email = user.Email,
            MfaEnabled = user.MfaEnabled,
            HasPassword = HasOwnPassword(user)
        };
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
    {
        var user = await _context.Users.FindAsync(userId);

        // Sin contraseña que cambiar (cuentas de Google) o compartida (demo).
        if (user is null
            || string.IsNullOrEmpty(user.PasswordHash)
            || user.Email == DemoDataSeeder.DemoEmail)
        {
            return new ChangePasswordResult { Outcome = ChangePasswordOutcome.NotAvailable };
        }

        // Mismo bloqueo y mismo contador que el login: con un JWT robado no se
        // pueden probar contraseñas aqui sin acabar bloqueando la cuenta.
        if (user.LockoutEnd > DateTime.UtcNow)
        {
            return new ChangePasswordResult { Outcome = ChangePasswordOutcome.InvalidCurrentPassword };
        }

        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, changePasswordDto.CurrentPassword)
            == PasswordVerificationResult.Failed)
        {
            await RegisterFailedAttemptAsync(user);

            return new ChangePasswordResult { Outcome = ChangePasswordOutcome.InvalidCurrentPassword };
        }

        if (changePasswordDto.NewPassword == changePasswordDto.CurrentPassword)
        {
            return new ChangePasswordResult { Outcome = ChangePasswordOutcome.Unchanged };
        }

        if (await _passwordBreachChecker.IsBreachedAsync(changePasswordDto.NewPassword))
        {
            return new ChangePasswordResult { Outcome = ChangePasswordOutcome.PasswordBreached };
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, changePasswordDto.NewPassword);
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        SessionService.Revoke(user);
        await _context.SaveChangesAsync();

        return new ChangePasswordResult { Outcome = ChangePasswordOutcome.Changed, User = ToDto(user) };
    }

    /// <summary>Ver UserDto.HasPassword. Las consultas EF repiten la condicion: no pueden llamar a un metodo.</summary>
    internal static bool HasOwnPassword(User user) =>
        !string.IsNullOrEmpty(user.PasswordHash) && user.Email != DemoDataSeeder.DemoEmail;

    // Contraseña incorrecta: cuenta para el bloqueo de la cuenta.
    private async Task RegisterFailedAttemptAsync(User user)
    {
        user.FailedLoginCount++;

        if (user.FailedLoginCount >= MaxFailedLogins)
        {
            user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
            user.FailedLoginCount = 0;
        }

        await _context.SaveChangesAsync();
    }

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        MfaEnabled = user.MfaEnabled,
        HasPassword = HasOwnPassword(user)
    };
}
