using System.Security.Cryptography;
using System.Text;
using FinanceTracker.Application.Common;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Services;

public class PasswordResetService : IPasswordResetService
{
    // Mismos limites que el codigo de verificacion del correo.
    public static readonly TimeSpan CodeLifetime = EmailVerificationService.CodeLifetime;
    public const int MaxAttempts = EmailVerificationService.MaxAttempts;
    public static readonly TimeSpan ResendInterval = EmailVerificationService.ResendInterval;

    private readonly AppDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IPasswordBreachChecker _passwordBreachChecker;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public PasswordResetService(
        AppDbContext context,
        IEmailSender emailSender,
        IPasswordBreachChecker passwordBreachChecker)
    {
        _context = context;
        _emailSender = emailSender;
        _passwordBreachChecker = passwordBreachChecker;
    }

    public async Task RequestAsync(string email)
    {
        var user = await FindAsync(email);

        // Sin cuenta, sin contraseña propia (Google, demo) o pedido hace menos
        // de un minuto: no se envia nada, pero quien llama no lo sabe. Asi el
        // endpoint no sirve para averiguar que correos estan registrados ni
        // para llenar de correos el buzon de nadie.
        if (user is null
            || !UserService.HasOwnPassword(user)
            || user.PasswordResetSentAt > DateTime.UtcNow - ResendInterval)
        {
            return;
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var now = DateTime.UtcNow;

        // Solo se guarda el hash: quien lea la base de datos no puede cambiar
        // contraseñas ajenas.
        user.PasswordResetCodeHash = HashCode(user.Id, code);
        user.PasswordResetExpiresAt = now.Add(CodeLifetime);
        user.PasswordResetSentAt = now;
        user.PasswordResetAttempts = 0;
        await _context.SaveChangesAsync();

        await _emailSender.SendPasswordResetCodeAsync(user.Email, code);
    }

    public async Task<PasswordResetOutcome> ResetAsync(string email, string code, string newPassword)
    {
        var user = await FindAsync(email);

        // Igual que un codigo incorrecto: no dice si el correo tiene cuenta.
        if (user is null || !UserService.HasOwnPassword(user))
        {
            return PasswordResetOutcome.InvalidCode;
        }

        if (!HasActiveCode(user))
        {
            return PasswordResetOutcome.Expired;
        }

        // Comparacion en tiempo constante: el tiempo de respuesta no dice
        // cuantos caracteres coinciden.
        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(HashCode(user.Id, code)),
            Encoding.ASCII.GetBytes(user.PasswordResetCodeHash!));

        if (!matches)
        {
            user.PasswordResetAttempts++;
            await _context.SaveChangesAsync();

            return user.PasswordResetAttempts >= MaxAttempts
                ? PasswordResetOutcome.Expired
                : PasswordResetOutcome.InvalidCode;
        }

        // Antes de gastar el codigo: con una contraseña filtrada se puede
        // probar otra sin pedir un codigo nuevo.
        if (await _passwordBreachChecker.IsBreachedAsync(newPassword))
        {
            return PasswordResetOutcome.PasswordBreached;
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

        // El codigo ha llegado al correo: el correo es suyo.
        user.EmailVerified = true;
        EmailVerificationService.ClearCode(user);

        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        ClearCode(user);

        // Si alguien tenia la contraseña vieja, pierde las sesiones que abrio.
        SessionService.Revoke(user);
        await _context.SaveChangesAsync();

        return PasswordResetOutcome.Reset;
    }

    private Task<User?> FindAsync(string email) =>
        _context.Users.FirstOrDefaultAsync(user => user.Email == email);

    private static void ClearCode(User user)
    {
        user.PasswordResetCodeHash = null;
        user.PasswordResetExpiresAt = null;
        user.PasswordResetSentAt = null;
        user.PasswordResetAttempts = 0;
    }

    private static bool HasActiveCode(User user) =>
        user.PasswordResetCodeHash is not null
        && user.PasswordResetExpiresAt > DateTime.UtcNow
        && user.PasswordResetAttempts < MaxAttempts;

    // "reset:" delante: el mismo codigo no da el mismo hash que uno de verificacion.
    private static string HashCode(int userId, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"reset:{userId}:{code}")));
}
