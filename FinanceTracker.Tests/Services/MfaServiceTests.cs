using System.Security.Cryptography;
using FinanceTracker.Application.Common;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Tests.Services;

public class MfaServiceTests
{
    private static readonly MfaSecretProtector Protector =
        new(RandomNumberGenerator.GetBytes(MfaSecretProtector.KeySize));

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<User> AddUser(AppDbContext context, string email = "antonio@test.com", string storedHash = "hashed-password")
    {
        var user = new User { Name = "Antonio", Email = email, PasswordHash = storedHash };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    // Codigo de la app autenticadora para el secreto devuelto por SetupAsync.
    private static string CodeFor(string base32Secret, int stepOffset = 0) =>
        Totp.ComputeCode(Totp.FromBase32(base32Secret), Totp.GetStep(DateTimeOffset.UtcNow) + stepOffset);

    [Fact]
    public async Task SetupAsync_ShouldReturnSecretAndUri_AndStoreTheSecretEncrypted()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var service = new MfaService(context, Protector);

        var result = await service.SetupAsync(user.Id);

        Assert.Equal(MfaOutcome.Success, result.Outcome);
        Assert.StartsWith("otpauth://totp/FinanceTracker%3Aantonio%40test.com?secret=", result.Value!.OtpAuthUri);
        Assert.NotEqual(result.Value.Secret, user.MfaSecret);
        Assert.Equal(result.Value.Secret, Protector.Unprotect(user.MfaSecret!));
        Assert.False(user.MfaEnabled);
    }

    [Theory]
    [InlineData("google@test.com", "")]
    [InlineData(DemoDataSeeder.DemoEmail, "hashed-password")]
    public async Task SetupAsync_ShouldNotBeAvailable_ForGoogleOnlyOrDemoAccounts(string email, string storedHash)
    {
        using var context = CreateDbContext();
        var user = await AddUser(context, email, storedHash);
        var service = new MfaService(context, Protector);

        var result = await service.SetupAsync(user.Id);

        Assert.Equal(MfaOutcome.NotAvailable, result.Outcome);
        Assert.Null(user.MfaSecret);
    }

    [Fact]
    public async Task ConfirmAsync_ShouldEnableMfa_AndReturnRecoveryCodes_WhenCodeIsValid()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var service = new MfaService(context, Protector);
        var setup = await service.SetupAsync(user.Id);

        var result = await service.ConfirmAsync(user.Id, CodeFor(setup.Value!.Secret));

        Assert.Equal(MfaOutcome.Success, result.Outcome);
        Assert.True(user.MfaEnabled);
        Assert.Equal(MfaService.RecoveryCodeCount, result.Value!.RecoveryCodes.Distinct().Count());

        // Solo se guardan los hashes.
        var stored = await context.MfaRecoveryCodes.Select(code => code.CodeHash).ToListAsync();
        Assert.Equal(
            result.Value.RecoveryCodes.Select(MfaService.HashRecoveryCode).OrderBy(hash => hash),
            stored.OrderBy(hash => hash));
    }

    [Fact]
    public async Task ConfirmAsync_ShouldRejectAWrongCode()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var service = new MfaService(context, Protector);
        var setup = await service.SetupAsync(user.Id);

        // Un codigo de hace 5 minutos: valido para ese secreto, pero caducado.
        var result = await service.ConfirmAsync(user.Id, CodeFor(setup.Value!.Secret, stepOffset: -10));

        Assert.Equal(MfaOutcome.InvalidCode, result.Outcome);
        Assert.False(user.MfaEnabled);
        Assert.Empty(context.MfaRecoveryCodes);
    }

    [Fact]
    public async Task ConfirmAsync_ShouldFail_WhenSetupWasNotStarted()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var service = new MfaService(context, Protector);

        var result = await service.ConfirmAsync(user.Id, "123456");

        Assert.Equal(MfaOutcome.SetupNotStarted, result.Outcome);
    }

    [Fact]
    public async Task DisableAsync_ShouldTurnOffMfa_AndDeleteRecoveryCodes_WithAValidCode()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var service = new MfaService(context, Protector);
        var setup = await service.SetupAsync(user.Id);
        await service.ConfirmAsync(user.Id, CodeFor(setup.Value!.Secret));

        // El siguiente intervalo: el actual ya se gasto al confirmar.
        var outcome = await service.DisableAsync(user.Id, CodeFor(setup.Value.Secret, stepOffset: 1));

        Assert.Equal(MfaOutcome.Success, outcome);
        Assert.False(user.MfaEnabled);
        Assert.Null(user.MfaSecret);
        Assert.Empty(context.MfaRecoveryCodes);
    }

    // Usuario con MFA ya activo; devuelve el secreto y los codigos de recuperacion.
    private static async Task<(User User, MfaService Service, string Secret, List<string> RecoveryCodes)> UserWithMfa(AppDbContext context)
    {
        var user = await AddUser(context);
        var service = new MfaService(context, Protector);
        var setup = await service.SetupAsync(user.Id);
        var confirm = await service.ConfirmAsync(user.Id, CodeFor(setup.Value!.Secret, stepOffset: -1));

        return (user, service, setup.Value.Secret, confirm.Value!.RecoveryCodes);
    }

    [Fact]
    public async Task VerifyLoginAsync_ShouldAcceptAValidTotpCode()
    {
        using var context = CreateDbContext();
        var (user, service, secret, _) = await UserWithMfa(context);

        var result = await service.VerifyLoginAsync(user.Id, CodeFor(secret));

        Assert.Equal(MfaOutcome.Success, result.Outcome);
        Assert.Equal(user.Id, result.Value!.Id);
    }

    [Fact]
    public async Task VerifyLoginAsync_ShouldAcceptARecoveryCode_OnlyOnce()
    {
        using var context = CreateDbContext();
        var (user, service, _, recoveryCodes) = await UserWithMfa(context);

        // En mayusculas y sin guiones tambien vale.
        var typed = recoveryCodes[0].Replace("-", string.Empty).ToUpperInvariant();

        Assert.Equal(MfaOutcome.Success, (await service.VerifyLoginAsync(user.Id, typed)).Outcome);
        Assert.Equal(MfaOutcome.InvalidCode, (await service.VerifyLoginAsync(user.Id, typed)).Outcome);
    }

    [Fact]
    public async Task VerifyLoginAsync_ShouldLockTheAccount_AfterTooManyWrongCodes()
    {
        using var context = CreateDbContext();
        var (user, service, secret, _) = await UserWithMfa(context);

        for (var i = 0; i < UserService.MaxFailedLogins; i++)
        {
            await service.VerifyLoginAsync(user.Id, "aaaa-bbbb-cccc-dddd");
        }

        // Bloqueada: ni el codigo correcto entra.
        var result = await service.VerifyLoginAsync(user.Id, CodeFor(secret));

        Assert.Equal(MfaOutcome.InvalidCode, result.Outcome);
        Assert.True(user.LockoutEnd > DateTime.UtcNow);
    }

    [Fact]
    public async Task VerifyLoginAsync_ShouldFail_WhenMfaIsNotEnabled()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var service = new MfaService(context, Protector);

        var result = await service.VerifyLoginAsync(user.Id, "123456");

        Assert.Equal(MfaOutcome.NotEnabled, result.Outcome);
    }

    [Fact]
    public async Task DisableAsync_ShouldRejectACodeThatWasAlreadyUsed()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var service = new MfaService(context, Protector);
        var setup = await service.SetupAsync(user.Id);
        var code = CodeFor(setup.Value!.Secret);
        await service.ConfirmAsync(user.Id, code);

        var outcome = await service.DisableAsync(user.Id, code);

        Assert.Equal(MfaOutcome.InvalidCode, outcome);
        Assert.True(user.MfaEnabled);
    }
}
