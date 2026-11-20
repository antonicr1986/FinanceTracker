using FinanceTracker.Api.Controllers;
using FinanceTracker.Application.Common;
using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Tests.Api;

public class PasswordResetControllerTests
{
    private class FakePasswordReset(PasswordResetOutcome outcome) : IPasswordResetService
    {
        public List<string> Requested { get; } = [];

        public Task RequestAsync(string email)
        {
            Requested.Add(email);
            return Task.CompletedTask;
        }

        public Task<PasswordResetOutcome> ResetAsync(string email, string code, string newPassword) =>
            Task.FromResult(outcome);
    }

    private static ResetPasswordDto ResetBody() =>
        new() { Email = "ana@test.com", Code = "123456", NewPassword = "una-clave-nueva-larga" };

    [Fact]
    public async Task Forgot_AlwaysAnswersNoContent()
    {
        // Exista o no la cuenta: la respuesta no dice que correos estan registrados.
        var service = new FakePasswordReset(PasswordResetOutcome.Reset);
        var controller = new PasswordResetController(service);

        var result = await controller.Forgot(new ForgotPasswordDto { Email = "ana@test.com" });

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(["ana@test.com"], service.Requested);
    }

    [Fact]
    public async Task Reset_WithTheRightCode_AnswersNoContent()
    {
        var controller = new PasswordResetController(new FakePasswordReset(PasswordResetOutcome.Reset));

        Assert.IsType<NoContentResult>(await controller.Reset(ResetBody()));
    }

    [Theory]
    [InlineData(PasswordResetOutcome.InvalidCode, "invalid_reset_code")]
    [InlineData(PasswordResetOutcome.Expired, "reset_code_expired")]
    [InlineData(PasswordResetOutcome.PasswordBreached, "password_breached")]
    public async Task Reset_Failures_AreBadRequestsWithTheirCode(PasswordResetOutcome outcome, string code)
    {
        var controller = new PasswordResetController(new FakePasswordReset(outcome));

        var problem = Assert.IsType<ObjectResult>(await controller.Reset(ResetBody()));

        Assert.Equal(400, problem.StatusCode);
        Assert.Equal(code, Assert.IsType<ProblemDetails>(problem.Value).Extensions["code"]);
    }

    [Fact]
    public void ResetEmail_CarriesTheCode_InBothLanguages()
    {
        var text = PasswordResetEmail.PlainText("123456");
        var html = PasswordResetEmail.Html("123456");

        Assert.Contains("123456", text);
        Assert.Contains("contraseña", text);
        Assert.Contains("password", text);
        Assert.Contains("123456", html);
    }
}
