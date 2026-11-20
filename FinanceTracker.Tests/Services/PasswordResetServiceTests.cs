using FinanceTracker.Application.Common;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Tests.Services;

public class PasswordResetServiceTests
{
    private const string Email = "antonio@test.com";
    private const string NewPassword = "una-clave-nueva-larga"; // gitleaks:allow — dato de prueba

    // Hace de correo: guarda los codigos de recuperacion "enviados".
    private class FakeEmailSender : IEmailSender
    {
        public List<(string Email, string Code)> Sent { get; } = [];

        public string LastCode => Sent[^1].Code;

        public Task SendVerificationCodeAsync(string email, string code) =>
            throw new InvalidOperationException("La recuperacion no envia codigos de verificacion");

        public Task SendPasswordResetCodeAsync(string email, string code)
        {
            Sent.Add((email, code));
            return Task.CompletedTask;
        }
    }

    private class FakeBreachChecker(bool isBreached) : IPasswordBreachChecker
    {
        public Task<bool> IsBreachedAsync(string password) => Task.FromResult(isBreached);
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options);

    private static async Task<User> AddUser(AppDbContext context, string passwordHash = "hash", string email = Email)
    {
        var user = new User { Name = "Antonio", Email = email, PasswordHash = passwordHash };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static PasswordResetService CreateService(
        AppDbContext context, FakeEmailSender sender, bool breached = false) =>
        new(context, sender, new FakeBreachChecker(breached));

    private static string WrongCode(string code) => code == "000000" ? "111111" : "000000";

    [Fact]
    public async Task Request_SendsASixDigitCode_AndStoresOnlyItsHash()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var sender = new FakeEmailSender();

        await CreateService(context, sender).RequestAsync(Email);

        var (email, code) = Assert.Single(sender.Sent);
        Assert.Equal(Email, email);
        Assert.Matches(@"^\d{6}$", code);
        Assert.NotNull(user.PasswordResetCodeHash);
        Assert.DoesNotContain(code, user.PasswordResetCodeHash);
    }

    [Fact]
    public async Task Request_ForAnUnknownEmail_SendsNothing()
    {
        using var context = CreateDbContext();
        var sender = new FakeEmailSender();

        await CreateService(context, sender).RequestAsync("nadie@test.com");

        Assert.Empty(sender.Sent);
    }

    [Theory]
    [InlineData("", Email)] // solo entra con Google
    [InlineData("hash", "demo@financetracker.app")] // cuenta demo compartida
    public async Task Request_ForAccountsWithoutTheirOwnPassword_SendsNothing(string passwordHash, string email)
    {
        using var context = CreateDbContext();
        await AddUser(context, passwordHash, email);
        var sender = new FakeEmailSender();

        await CreateService(context, sender).RequestAsync(email);

        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task Request_TwiceWithinAMinute_SendsOnlyOneCode()
    {
        using var context = CreateDbContext();
        await AddUser(context);
        var sender = new FakeEmailSender();
        var service = CreateService(context, sender);

        await service.RequestAsync(Email);
        await service.RequestAsync(Email);

        Assert.Single(sender.Sent);
    }

    [Fact]
    public async Task Reset_WithTheRightCode_ChangesThePassword_VerifiesTheEmail_AndClosesEverything()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        user.FailedLoginCount = 3;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
        user.EmailCodeHash = "pendiente";
        await context.SaveChangesAsync();
        var sender = new FakeEmailSender();
        var service = CreateService(context, sender);
        await service.RequestAsync(Email);

        var outcome = await service.ResetAsync(Email, sender.LastCode, NewPassword);

        Assert.Equal(PasswordResetOutcome.Reset, outcome);
        Assert.NotEqual(PasswordVerificationResult.Failed,
            new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash!, NewPassword));
        Assert.True(user.EmailVerified);
        Assert.Null(user.EmailCodeHash);
        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockoutEnd);
        Assert.NotNull(user.SessionsRevokedAt);
        Assert.Null(user.PasswordResetCodeHash);
    }

    [Fact]
    public async Task Reset_UsesTheCodeOnlyOnce()
    {
        using var context = CreateDbContext();
        await AddUser(context);
        var sender = new FakeEmailSender();
        var service = CreateService(context, sender);
        await service.RequestAsync(Email);
        var code = sender.LastCode;

        await service.ResetAsync(Email, code, NewPassword);
        var again = await service.ResetAsync(Email, code, "otra-clave-larga-mas");

        Assert.Equal(PasswordResetOutcome.Expired, again);
    }

    [Fact]
    public async Task Reset_WithAWrongCode_CountsTheAttempt_AndExpiresAfterTheMaximum()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var sender = new FakeEmailSender();
        var service = CreateService(context, sender);
        await service.RequestAsync(Email);
        var wrong = WrongCode(sender.LastCode);

        for (var attempt = 1; attempt < PasswordResetService.MaxAttempts; attempt++)
        {
            Assert.Equal(PasswordResetOutcome.InvalidCode, await service.ResetAsync(Email, wrong, NewPassword));
        }

        Assert.Equal(PasswordResetOutcome.Expired, await service.ResetAsync(Email, wrong, NewPassword));

        // Agotados los intentos, ni el codigo bueno vale.
        Assert.Equal(PasswordResetOutcome.Expired, await service.ResetAsync(Email, sender.LastCode, NewPassword));
        Assert.Equal("hash", user.PasswordHash);
    }

    [Fact]
    public async Task Reset_AfterTheCodeExpires_AsksForANewOne()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var sender = new FakeEmailSender();
        var service = CreateService(context, sender);
        await service.RequestAsync(Email);
        user.PasswordResetExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        await context.SaveChangesAsync();

        Assert.Equal(PasswordResetOutcome.Expired, await service.ResetAsync(Email, sender.LastCode, NewPassword));
    }

    [Fact]
    public async Task Reset_WithABreachedPassword_KeepsTheCode_SoAnotherPasswordCanBeTried()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var sender = new FakeEmailSender();
        await CreateService(context, sender).RequestAsync(Email);

        var breached = await CreateService(context, sender, breached: true)
            .ResetAsync(Email, sender.LastCode, NewPassword);
        var retried = await CreateService(context, sender).ResetAsync(Email, sender.LastCode, NewPassword);

        Assert.Equal(PasswordResetOutcome.PasswordBreached, breached);
        Assert.Equal(PasswordResetOutcome.Reset, retried);
    }

    [Fact]
    public async Task Reset_ForAnUnknownEmail_LooksLikeAWrongCode()
    {
        using var context = CreateDbContext();

        var outcome = await CreateService(context, new FakeEmailSender())
            .ResetAsync("nadie@test.com", "123456", NewPassword);

        Assert.Equal(PasswordResetOutcome.InvalidCode, outcome);
    }

    [Fact]
    public async Task AVerificationCode_DoesNotWorkForAReset()
    {
        // Los dos codigos se guardan con hashes distintos: uno no sirve para lo otro.
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var sender = new FakeEmailSender();
        var service = CreateService(context, sender);
        await service.RequestAsync(Email);
        user.EmailCodeHash = user.PasswordResetCodeHash;
        user.EmailCodeExpiresAt = user.PasswordResetExpiresAt;
        await context.SaveChangesAsync();

        var verification = new EmailVerificationService(context, new ThrowingSender());
        Assert.Equal(EmailVerificationOutcome.InvalidCode, await verification.VerifyAsync(user.Id, sender.LastCode));
    }

    private class ThrowingSender : IEmailSender
    {
        public Task SendVerificationCodeAsync(string email, string code) => throw new InvalidOperationException();

        public Task SendPasswordResetCodeAsync(string email, string code) => throw new InvalidOperationException();
    }
}
