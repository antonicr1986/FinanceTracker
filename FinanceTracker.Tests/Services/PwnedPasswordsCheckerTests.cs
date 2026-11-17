using System.Net;
using FinanceTracker.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinanceTracker.Tests.Services;

public class PwnedPasswordsCheckerTests
{
    // SHA-1 de "password": 5BAA6 + 1E4C9B93F3F0682250B6CF8331B7EE68FD8.
    private const string PasswordSuffix = "1E4C9B93F3F0682250B6CF8331B7EE68FD8";

    // Sustituye a la API real: responde siempre lo mismo y guarda la URL pedida.
    private class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _respond;

        public StubHandler(Func<HttpResponseMessage> respond) => _respond = respond;

        public Uri? RequestedUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUri = request.RequestUri;
            return Task.FromResult(_respond());
        }
    }

    private static (PwnedPasswordsChecker Checker, StubHandler Handler) CheckerReturning(Func<HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.pwnedpasswords.com/") };

        return (new PwnedPasswordsChecker(httpClient, NullLogger<PwnedPasswordsChecker>.Instance), handler);
    }

    private static HttpResponseMessage Body(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body) };

    [Fact]
    public async Task IsBreachedAsync_ShouldReturnTrue_WhenSuffixIsListed()
    {
        var (checker, _) = CheckerReturning(() => Body($"0018A45C4D1DEF81644B54AB7F969B88D65:1\r\n{PasswordSuffix}:9545824\r\n"));

        Assert.True(await checker.IsBreachedAsync("password"));
    }

    [Fact]
    public async Task IsBreachedAsync_ShouldSendOnlyTheFirstFiveHashCharacters()
    {
        var (checker, handler) = CheckerReturning(() => Body(""));

        await checker.IsBreachedAsync("password");

        Assert.Equal("https://api.pwnedpasswords.com/range/5BAA6", handler.RequestedUri!.ToString());
    }

    [Fact]
    public async Task IsBreachedAsync_ShouldReturnFalse_WhenSuffixIsNotListed()
    {
        var (checker, _) = CheckerReturning(() => Body("0018A45C4D1DEF81644B54AB7F969B88D65:1\r\n"));

        Assert.False(await checker.IsBreachedAsync("password"));
    }

    [Fact]
    public async Task IsBreachedAsync_ShouldIgnorePaddingLines_WithZeroCount()
    {
        var (checker, _) = CheckerReturning(() => Body($"{PasswordSuffix}:0\r\n"));

        Assert.False(await checker.IsBreachedAsync("password"));
    }

    [Fact]
    public async Task IsBreachedAsync_ShouldReturnFalse_WhenServiceIsDown()
    {
        var (checker, _) = CheckerReturning(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        Assert.False(await checker.IsBreachedAsync("password"));
    }
}
