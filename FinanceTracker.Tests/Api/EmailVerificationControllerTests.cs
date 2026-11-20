using FinanceTracker.Api.Controllers;
using FinanceTracker.Application.DTOs.Mfa;
using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FinanceTracker.Tests.Api;

/// <summary>Paso del codigo del correo, con los servicios reales y una base de datos en memoria.</summary>
public class EmailVerificationControllerTests
{
    private class FakeEmailSender : IEmailSender
    {
        public List<string> Codes { get; } = [];

        public Task SendVerificationCodeAsync(string email, string code)
        {
            Codes.Add(code);
            return Task.CompletedTask;
        }

        public Task SendPasswordResetCodeAsync(string email, string code) => throw new InvalidOperationException();
    }

    private class NeverBreached : IPasswordBreachChecker
    {
        public Task<bool> IsBreachedAsync(string password) => Task.FromResult(false);
    }

    private class NoGoogle : IGoogleTokenValidator
    {
        public Task<GoogleIdentity?> ValidateAsync(string idToken) => Task.FromResult<GoogleIdentity?>(null);
    }

    private sealed class Setup : IDisposable
    {
        public Setup()
        {
            Context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);
            Tokens = new TokenService(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "clave-solo-para-tests-clave-solo-para-tests-1234", // gitleaks:allow
                    ["Jwt:Issuer"] = "FinanceTracker.Api",
                    ["Jwt:Audience"] = "FinanceTracker.Client"
                })
                .Build());
            Verification = new EmailVerificationService(Context, Sender);
            Controller = new EmailVerificationController(
                Verification, Tokens, new UserService(Context, new NeverBreached(), new NoGoogle()));
        }

        public AppDbContext Context { get; }
        public FakeEmailSender Sender { get; } = new();
        public TokenService Tokens { get; }
        public EmailVerificationService Verification { get; }
        public EmailVerificationController Controller { get; }

        public async Task<(User User, string Token)> UnverifiedUserWithCode(bool mfa = false)
        {
            var user = new User { Name = "Ana", Email = "ana@test.com", PasswordHash = "hash", MfaEnabled = mfa };
            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            await Verification.EnsureCodeSentAsync(user.Id);
            return (user, Tokens.CreateEmailVerificationChallenge(user.Id, user.Email).VerificationToken);
        }

        public void Dispose() => Context.Dispose();
    }

    private static string? ProblemCode(IActionResult? result) =>
        Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(result).Value).Extensions["code"] as string;

    [Fact]
    public async Task Verify_WithTheRightCode_StartsTheSession()
    {
        using var setup = new Setup();
        var (user, token) = await setup.UnverifiedUserWithCode();

        var result = await setup.Controller.Verify(new EmailVerifyDto { VerificationToken = token, Code = setup.Sender.Codes[^1] });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.False(string.IsNullOrEmpty(Assert.IsType<LoginResponseDto>(ok.Value).Token));
        Assert.True(user.EmailVerified);
    }

    [Fact]
    public async Task Verify_ForAnAccountWithMfa_AsksForTheMfaCodeNext()
    {
        using var setup = new Setup();
        var (_, token) = await setup.UnverifiedUserWithCode(mfa: true);

        var result = await setup.Controller.Verify(new EmailVerifyDto { VerificationToken = token, Code = setup.Sender.Codes[^1] });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<MfaChallengeDto>(ok.Value).MfaRequired);
    }

    [Fact]
    public async Task Verify_WithAWrongCode_IsABadRequest()
    {
        using var setup = new Setup();
        var (_, token) = await setup.UnverifiedUserWithCode();
        var wrong = setup.Sender.Codes[^1] == "000000" ? "111111" : "000000";

        var result = await setup.Controller.Verify(new EmailVerifyDto { VerificationToken = token, Code = wrong });

        Assert.Equal("invalid_email_code", ProblemCode(result.Result));
    }

    [Fact]
    public async Task Verify_AfterTheCodeExpires_AsksForANewOne()
    {
        using var setup = new Setup();
        var (user, token) = await setup.UnverifiedUserWithCode();
        user.EmailCodeExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        await setup.Context.SaveChangesAsync();

        var result = await setup.Controller.Verify(new EmailVerifyDto { VerificationToken = token, Code = setup.Sender.Codes[^1] });

        Assert.Equal("email_code_expired", ProblemCode(result.Result));
    }

    [Fact]
    public async Task Verify_AndResend_WithABadToken_AreUnauthorized()
    {
        using var setup = new Setup();

        var verify = await setup.Controller.Verify(new EmailVerifyDto { VerificationToken = "no-vale", Code = "123456" });
        var resend = await setup.Controller.Resend(new EmailResendDto { VerificationToken = "no-vale" });

        Assert.Equal("invalid_verification_token", ProblemCode(verify.Result));
        Assert.Equal("invalid_verification_token", ProblemCode(resend));
    }

    [Fact]
    public async Task Resend_RightAway_IsRejected_ButLaterSendsANewCode()
    {
        using var setup = new Setup();
        var (user, token) = await setup.UnverifiedUserWithCode();

        Assert.Equal("email_code_recently_sent",
            ProblemCode(await setup.Controller.Resend(new EmailResendDto { VerificationToken = token })));

        user.EmailCodeSentAt = DateTime.UtcNow.AddMinutes(-2);
        await setup.Context.SaveChangesAsync();

        Assert.IsType<NoContentResult>(await setup.Controller.Resend(new EmailResendDto { VerificationToken = token }));
        Assert.Equal(2, setup.Sender.Codes.Count);
    }
}
