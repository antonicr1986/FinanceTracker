using System.Security.Cryptography;
using System.Text;
using FinanceTracker.Application.Common;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Services;

public class EmailVerificationService : IEmailVerificationService
{
    // Cuanto vale un codigo, cuantos fallos admite y cada cuanto se puede pedir otro.
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    public const int MaxAttempts = 5;
    public static readonly TimeSpan ResendInterval = TimeSpan.FromSeconds(60);

    private readonly AppDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly bool _required;

    /// <param name="required">
    /// Email:RequireVerification. Se activa cuando el envio de correos esta
    /// configurado y los clientes saben pedir el codigo; hasta entonces la API
    /// se comporta como antes.
    /// </param>
    public EmailVerificationService(AppDbContext context, IEmailSender emailSender, bool required = true)
    {
        _context = context;
        _emailSender = emailSender;
        _required = required;
    }

    public async Task<bool> IsVerifiedAsync(int userId) =>
        !_required || await _context.Users
            .Where(user => user.Id == userId)
            .Select(user => user.EmailVerified)
            .FirstOrDefaultAsync();

    public async Task OnRegisteredAsync(int userId)
    {
        if (_required)
        {
            return;
        }

        var user = await _context.Users.FindAsync(userId);

        if (user is not null && !user.EmailVerified)
        {
            user.EmailVerified = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task EnsureCodeSentAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user is null || user.EmailVerified || HasActiveCode(user))
        {
            return;
        }

        await SendNewCodeAsync(user);
    }

    public async Task<EmailVerificationOutcome> ResendAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user is null || user.EmailVerified)
        {
            return EmailVerificationOutcome.Verified;
        }

        // Sin limite, el endpoint serviria para llenar de correos el buzon de
        // cualquiera (o gastar el envio de correos de la aplicacion).
        if (user.EmailCodeSentAt > DateTime.UtcNow - ResendInterval)
        {
            return EmailVerificationOutcome.RecentlySent;
        }

        await SendNewCodeAsync(user);

        return EmailVerificationOutcome.Sent;
    }

    public async Task<EmailVerificationOutcome> VerifyAsync(int userId, string code)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user is null)
        {
            return EmailVerificationOutcome.Expired;
        }

        if (user.EmailVerified)
        {
            return EmailVerificationOutcome.Verified;
        }

        if (!HasActiveCode(user))
        {
            return EmailVerificationOutcome.Expired;
        }

        // Comparacion en tiempo constante: el tiempo de respuesta no dice
        // cuantos caracteres coinciden.
        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(HashCode(user.Id, code)),
            Encoding.ASCII.GetBytes(user.EmailCodeHash!));

        if (!matches)
        {
            // Un codigo de 6 digitos son un millon de opciones: con 5 intentos
            // por codigo, adivinarlo es practicamente imposible.
            user.EmailCodeAttempts++;
            await _context.SaveChangesAsync();

            return user.EmailCodeAttempts >= MaxAttempts
                ? EmailVerificationOutcome.Expired
                : EmailVerificationOutcome.InvalidCode;
        }

        user.EmailVerified = true;
        ClearCode(user);
        await _context.SaveChangesAsync();

        return EmailVerificationOutcome.Verified;
    }

    /// <summary>Borra el codigo pendiente (al verificar o al rehacer el registro).</summary>
    public static void ClearCode(User user)
    {
        user.EmailCodeHash = null;
        user.EmailCodeExpiresAt = null;
        user.EmailCodeSentAt = null;
        user.EmailCodeAttempts = 0;
    }

    private static bool HasActiveCode(User user) =>
        user.EmailCodeHash is not null
        && user.EmailCodeExpiresAt > DateTime.UtcNow
        && user.EmailCodeAttempts < MaxAttempts;

    private async Task SendNewCodeAsync(User user)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var now = DateTime.UtcNow;

        // Solo se guarda el hash: quien lea la base de datos no puede verificar
        // cuentas ajenas.
        user.EmailCodeHash = HashCode(user.Id, code);
        user.EmailCodeExpiresAt = now.Add(CodeLifetime);
        user.EmailCodeSentAt = now;
        user.EmailCodeAttempts = 0;
        await _context.SaveChangesAsync();

        await _emailSender.SendVerificationCodeAsync(user.Email, code);
    }

    // Con el id delante: el mismo codigo da hashes distintos en cuentas distintas.
    private static string HashCode(int userId, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}:{code}")));
}
