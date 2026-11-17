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
