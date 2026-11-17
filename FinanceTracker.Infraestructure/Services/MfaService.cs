using System.Security.Cryptography;
using System.Text;
using FinanceTracker.Application.Common;
using System.Text.RegularExpressions;
using FinanceTracker.Application.DTOs.Mfa;
using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Services;

/// <summary>
/// MFA opcional con app autenticadora (TOTP). Activarlo tiene dos pasos:
/// SetupAsync genera el secreto (y el QR) y ConfirmAsync lo activa cuando el
/// usuario demuestra, con un primer codigo, que lo ha guardado bien.
/// </summary>
public class MfaService : IMfaService
{
    public const int RecoveryCodeCount = 10;

    private const string Issuer = "FinanceTracker";

    private readonly AppDbContext _context;
    private readonly MfaSecretProtector _protector;

    public MfaService(AppDbContext context, MfaSecretProtector protector)
    {
        _context = context;
        _protector = protector;
    }

    public async Task<MfaResult<MfaSetupDto>> SetupAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        // Sin contraseña (solo Google) no hay nada que reforzar: ya tiene el
        // MFA de Google. La demo tiene credenciales publicas: si alguien le
        // activara el MFA, dejaria fuera a todos los demas.
        if (user is null
            || string.IsNullOrEmpty(user.PasswordHash)
            || user.Email == DemoDataSeeder.DemoEmail)
        {
            return MfaResult<MfaSetupDto>.Fail(MfaOutcome.NotAvailable);
        }

        if (user.MfaEnabled)
        {
            return MfaResult<MfaSetupDto>.Fail(MfaOutcome.AlreadyEnabled);
        }

        // Pedirlo otra vez sustituye el secreto pendiente: vale el ultimo QR.
        var secret = Totp.ToBase32(Totp.GenerateSecret());
        user.MfaSecret = _protector.Protect(secret);
        await _context.SaveChangesAsync();

        return MfaResult<MfaSetupDto>.Ok(new MfaSetupDto
        {
            Secret = secret,
            OtpAuthUri = BuildOtpAuthUri(user.Email, secret)
        });
    }

    public async Task<MfaResult<MfaRecoveryCodesDto>> ConfirmAsync(int userId, string code)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user is null)
        {
            return MfaResult<MfaRecoveryCodesDto>.Fail(MfaOutcome.NotAvailable);
        }

        if (user.MfaEnabled)
        {
            return MfaResult<MfaRecoveryCodesDto>.Fail(MfaOutcome.AlreadyEnabled);
        }

        if (user.MfaSecret is null)
        {
            return MfaResult<MfaRecoveryCodesDto>.Fail(MfaOutcome.SetupNotStarted);
        }

        var step = MatchCode(user, code);

        if (step is null)
        {
            return MfaResult<MfaRecoveryCodesDto>.Fail(MfaOutcome.InvalidCode);
        }

        user.MfaEnabled = true;
        user.MfaLastUsedStep = step;

        var recoveryCodes = Enumerable.Range(0, RecoveryCodeCount)
            .Select(_ => NewRecoveryCode())
            .ToList();

        _context.MfaRecoveryCodes.AddRange(recoveryCodes.Select(recoveryCode => new MfaRecoveryCode
        {
            UserId = user.Id,
            CodeHash = HashRecoveryCode(recoveryCode)
        }));

        await _context.SaveChangesAsync();

        return MfaResult<MfaRecoveryCodesDto>.Ok(new MfaRecoveryCodesDto { RecoveryCodes = recoveryCodes });
    }

    public async Task<MfaOutcome> DisableAsync(int userId, string code)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user is null || !user.MfaEnabled)
        {
            return MfaOutcome.NotEnabled;
        }

        // Se pide un codigo y no basta con la sesion: quien robe un JWT no
        // puede quitarle el MFA a la cuenta.
        if (MatchCode(user, code) is null)
        {
            return MfaOutcome.InvalidCode;
        }

        user.MfaEnabled = false;
        user.MfaSecret = null;
        user.MfaLastUsedStep = null;

        var recoveryCodes = await _context.MfaRecoveryCodes
            .Where(recoveryCode => recoveryCode.UserId == user.Id)
            .ToListAsync();

        _context.MfaRecoveryCodes.RemoveRange(recoveryCodes);
        await _context.SaveChangesAsync();

        return MfaOutcome.Success;
    }

    public async Task<MfaResult<UserDto>> VerifyLoginAsync(int userId, string code)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user is null || !user.MfaEnabled)
        {
            return MfaResult<UserDto>.Fail(MfaOutcome.NotEnabled);
        }

        // Bloqueada: igual que en el login, ni el codigo bueno entra.
        if (user.LockoutEnd > DateTime.UtcNow)
        {
            return MfaResult<UserDto>.Fail(MfaOutcome.InvalidCode);
        }

        var accepted = Regex.IsMatch(code, @"^\d{6}$")
            ? AcceptTotp(user, code)
            : await AcceptRecoveryCodeAsync(user.Id, code);

        if (!accepted)
        {
            // Mismo contador que las contraseñas: con la contraseña robada,
            // probar codigos tambien acaba bloqueando la cuenta.
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= UserService.MaxFailedLogins)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(UserService.LockoutDuration);
                user.FailedLoginCount = 0;
            }

            await _context.SaveChangesAsync();

            return MfaResult<UserDto>.Fail(MfaOutcome.InvalidCode);
        }

        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        await _context.SaveChangesAsync();

        return MfaResult<UserDto>.Ok(new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            MfaEnabled = true
        });
    }

    private bool AcceptTotp(User user, string code)
    {
        var step = MatchCode(user, code);

        if (step is null)
        {
            return false;
        }

        user.MfaLastUsedStep = step;

        return true;
    }

    // Cada codigo de recuperacion sirve una sola vez: se marca como usado.
    private async Task<bool> AcceptRecoveryCodeAsync(int userId, string code)
    {
        var hash = HashRecoveryCode(code);

        var recoveryCode = await _context.MfaRecoveryCodes
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId
                && candidate.UsedAt == null
                && candidate.CodeHash == hash);

        if (recoveryCode is null)
        {
            return false;
        }

        recoveryCode.UsedAt = DateTime.UtcNow;

        return true;
    }

    /// <summary>
    /// Intervalo del codigo si es valido y no se habia usado ya. Un codigo
    /// capturado (por encima del hombro, por phishing) no sirve dos veces.
    /// </summary>
    private long? MatchCode(User user, string code)
    {
        var secret = Totp.FromBase32(_protector.Unprotect(user.MfaSecret!));
        var step = Totp.FindMatchingStep(secret, code, DateTimeOffset.UtcNow);

        if (step is null || step <= user.MfaLastUsedStep)
        {
            return null;
        }

        return step;
    }

    private static string BuildOtpAuthUri(string email, string secret)
    {
        var label = Uri.EscapeDataString($"{Issuer}:{email}");

        return $"otpauth://totp/{label}?secret={secret}&issuer={Issuer}&algorithm=SHA1&digits={Totp.Digits}&period={Totp.StepSeconds}";
    }

    // 80 bits aleatorios en base32, en grupos de 4: "abcd-efgh-ijkl-mnop".
    private static string NewRecoveryCode()
    {
        var text = Totp.ToBase32(RandomNumberGenerator.GetBytes(10)).ToLowerInvariant();

        return string.Join('-', Enumerable.Range(0, 4).Select(group => text.Substring(group * 4, 4)));
    }

    /// <summary>
    /// SHA-256 y no PBKDF2 como las contraseñas: estos codigos son aleatorios
    /// de 80 bits, no algo que una persona elige, asi que no hay diccionario
    /// que probar y un hash rapido basta. Se normaliza para aceptarlos con o
    /// sin guiones y en mayusculas o minusculas.
    /// </summary>
    public static string HashRecoveryCode(string recoveryCode)
    {
        var normalized = recoveryCode.Replace("-", string.Empty).Trim().ToUpperInvariant();

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
