using System.Security.Claims;
using System.Text.Json;
using FinanceTracker.Api.Middleware;
using FinanceTracker.Api.Services;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinanceTracker.Tests.Api;

/// <summary>Piezas sueltas de la API: siembra de la demo, errores, usuario actual y correos sin servicio.</summary>
public class InfrastructurePiecesTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options);

    // --- Cuenta demo ---

    [Fact]
    public async Task DemoSeeder_CreatesAVerifiedDemoAccount_WithData()
    {
        using var context = CreateDbContext();

        await DemoDataSeeder.SeedAsync(context);

        var demo = await context.Users.SingleAsync();
        Assert.Equal(DemoDataSeeder.DemoEmail, demo.Email);
        Assert.True(demo.EmailVerified);
        Assert.NotEqual(PasswordVerificationResult.Failed,
            new PasswordHasher<FinanceTracker.Domain.Entities.User>()
                .VerifyHashedPassword(demo, demo.PasswordHash, DemoDataSeeder.DemoPassword));
        Assert.True(await context.Categories.AnyAsync(category => category.UserId == demo.Id));
        Assert.True(await context.Transactions.AnyAsync(transaction => transaction.UserId == demo.Id));
    }

    [Fact]
    public async Task DemoSeeder_RunTwice_DoesNotDuplicateAnything()
    {
        using var context = CreateDbContext();

        await DemoDataSeeder.SeedAsync(context);
        var categories = await context.Categories.CountAsync();
        var transactions = await context.Transactions.CountAsync();
        await DemoDataSeeder.SeedAsync(context);

        Assert.Equal(1, await context.Users.CountAsync());
        Assert.Equal(categories, await context.Categories.CountAsync());
        Assert.Equal(transactions, await context.Transactions.CountAsync());
    }

    // --- Errores no controlados ---

    [Fact]
    public async Task ExceptionHandler_Answers500_WithoutLeakingTheException()
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/transactions";
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            context, new InvalidOperationException("secreto interno"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("secreto interno", body);
        Assert.Equal(500, JsonDocument.Parse(body).RootElement.GetProperty("status").GetInt32());
    }

    // --- Usuario actual ---

    private static CurrentUserService CurrentUser(params Claim[] claims) =>
        new(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
        });

    [Fact]
    public void CurrentUser_ReadsTheIdFromTheToken()
    {
        Assert.Equal(42, CurrentUser(new Claim(ClaimTypes.NameIdentifier, "42")).UserId);
    }

    [Fact]
    public void CurrentUser_WithoutAValidId_IsNull()
    {
        Assert.Null(CurrentUser().UserId);
        Assert.Null(CurrentUser(new Claim(ClaimTypes.NameIdentifier, "no-es-un-numero")).UserId);
        Assert.Null(new CurrentUserService(new HttpContextAccessor()).UserId);
    }

    // --- Correo sin servicio configurado ---

    private class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Entries.Add((logLevel, formatter(state, exception)));
    }

    [Fact]
    public async Task LoggingSender_InDevelopment_WritesTheCodes()
    {
        var logger = new CapturingLogger<LoggingEmailSender>();
        var sender = new LoggingEmailSender(logger, logCode: true);

        await sender.SendVerificationCodeAsync("ana@test.com", "123456");
        await sender.SendPasswordResetCodeAsync("ana@test.com", "654321");

        Assert.Contains(logger.Entries, entry => entry.Message.Contains("123456"));
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("654321"));
    }

    [Fact]
    public async Task LoggingSender_OutsideDevelopment_NeverWritesTheCodes()
    {
        // En cualquier otro entorno, un codigo en los logs dejaria usarlo a quien los lea.
        var logger = new CapturingLogger<LoggingEmailSender>();
        var sender = new LoggingEmailSender(logger, logCode: false);

        await sender.SendVerificationCodeAsync("ana@test.com", "123456");
        await sender.SendPasswordResetCodeAsync("ana@test.com", "654321");

        Assert.Equal(2, logger.Entries.Count);
        Assert.All(logger.Entries, entry =>
        {
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.DoesNotContain("123456", entry.Message);
            Assert.DoesNotContain("654321", entry.Message);
        });
    }

    [Fact]
    public void VerificationEmail_CarriesTheCode_InBothLanguages()
    {
        var text = VerificationEmail.PlainText("123456");

        Assert.Contains("123456", text);
        Assert.Contains("correo", text);
        Assert.Contains("email", text);
        Assert.Contains("123456", VerificationEmail.Html("123456"));
    }
}
