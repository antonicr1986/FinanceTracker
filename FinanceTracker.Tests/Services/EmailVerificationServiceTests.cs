using FinanceTracker.Application.Common;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Tests.Services;

public class EmailVerificationServiceTests
{
    // Hace de correo: guarda el ultimo codigo "enviado".
    private class FakeEmailSender : IEmailSender
    {
        public List<(string Email, string Code)> Sent { get; } = [];

        public string LastCode => Sent[^1].Code;

        public Task SendVerificationCodeAsync(string email, string code)
        {
            Sent.Add((email, code));
            return Task.CompletedTask;
        }
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options);

    private static async Task<User> AddUser(AppDbContext context, bool verified = false)
    {
        var user = new User { Name = "Antonio", Email = "antonio@test.com", PasswordHash = "hash", EmailVerified = verified };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task WhenNotRequired_EveryoneCountsAsVerified_AndNewAccountsAreMarkedVerified()
    {
        // Email:RequireVerification = false: la API se comporta como antes.
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var service = new EmailVerificationService(context, new FakeEmailSender(), required: false);

        Assert.True(await service.IsVerifiedAsync(user.Id));

        await service.OnRegisteredAsync(user.Id);
        Assert.True(user.EmailVerified);
    }

    [Fact]
    public async Task WhenRequired_ANewAccountStaysUnverified()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var service = new EmailVerificationService(context, new FakeEmailSender());

        await service.OnRegisteredAsync(user.Id);

        Assert.False(user.EmailVerified);
        Assert.False(await service.IsVerifiedAsync(user.Id));
    }

    [Fact]
    public async Task EnsureCodeSent_ShouldSendASixDigitCode_AndStoreOnlyItsHash()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var sender = new FakeEmailSender();

        await new EmailVerificationService(context, sender).EnsureCodeSentAsync(user.Id);

        var (email, code) = Assert.Single(sender.Sent);
        Assert.Equal("antonio@test.com", email);
        Assert.Matches(@"^\d{6}$", code);
        Assert.NotNull(user.EmailCodeHash);
        Assert.DoesNotContain(code, user.EmailCodeHash);
    }

    [Fact]
    public async Task EnsureCodeSent_ShouldNotSendAnother_WhileTheFirstIsValid()
    {
        // Volver a entrar no debe invalidar el codigo que ya esta en el correo.
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var sender = new FakeEmailSender();
        var service = new EmailVerificationService(context, sender);

        await service.EnsureCodeSentAsync(user.Id);
        await service.EnsureCodeSentAsync(user.Id);

        Assert.Single(sender.Sent);
    }

    [Fact]
    public async Task EnsureCodeSent_ShouldDoNothing_ForAVerifiedAccount()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context, verified: true);
        var sender = new FakeEmailSender();

        await new EmailVerificationService(context, sender).EnsureCodeSentAsync(user.Id);

        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task Verify_ShouldMarkTheEmailAsVerified_WithTheRightCode()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var sender = new FakeEmailSender();
        var service = new EmailVerificationService(context, sender);
        await service.EnsureCodeSentAsync(user.Id);

        var outcome = await service.VerifyAsync(user.Id, sender.LastCode);

        Assert.Equal(EmailVerificationOutcome.Verified, outcome);
        Assert.True(user.EmailVerified);
        Assert.Null(user.EmailCodeHash);
        Assert.True(await service.IsVerifiedAsync(user.Id));
    }

    [Fact]
    public async Task Verify_ShouldInvalidateTheCode_AfterTooManyWrongAttempts()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var sender = new FakeEmailSender();
        var service = new EmailVerificationService(context, sender);
        await service.EnsureCodeSentAsync(user.Id);
        var wrong = sender.LastCode == "000000" ? "111111" : "000000";

        for (var i = 1; i < EmailVerificationService.MaxAttempts; i++)
        {
            Assert.Equal(EmailVerificationOutcome.InvalidCode, await service.VerifyAsync(user.Id, wrong));
        }

        Assert.Equal(EmailVerificationOutcome.Expired, await service.VerifyAsync(user.Id, wrong));

        // Ni el bueno vale ya: hay que pedir otro.
        Assert.Equal(EmailVerificationOutcome.Expired, await service.VerifyAsync(user.Id, sender.LastCode));
        Assert.False(user.EmailVerified);
    }

    [Fact]
    public async Task Verify_ShouldRejectAnExpiredCode()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var sender = new FakeEmailSender();
        var service = new EmailVerificationService(context, sender);
        await service.EnsureCodeSentAsync(user.Id);

        user.EmailCodeExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        await context.SaveChangesAsync();

        Assert.Equal(EmailVerificationOutcome.Expired, await service.VerifyAsync(user.Id, sender.LastCode));
    }

    [Fact]
    public async Task Resend_ShouldWaitAMinute_AndThenSendANewCode()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var sender = new FakeEmailSender();
        var service = new EmailVerificationService(context, sender);
        await service.EnsureCodeSentAsync(user.Id);

        Assert.Equal(EmailVerificationOutcome.RecentlySent, await service.ResendAsync(user.Id));

        user.EmailCodeSentAt = DateTime.UtcNow - EmailVerificationService.ResendInterval;
        await context.SaveChangesAsync();
        var firstCode = sender.LastCode;

        Assert.Equal(EmailVerificationOutcome.Sent, await service.ResendAsync(user.Id));
        Assert.Equal(2, sender.Sent.Count);

        // El anterior deja de valer (salvo que coincida por azar, 1 entre un millon).
        if (sender.LastCode != firstCode)
        {
            Assert.Equal(EmailVerificationOutcome.InvalidCode, await service.VerifyAsync(user.Id, firstCode));
        }
    }
}
