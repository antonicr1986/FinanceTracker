using FinanceTracker.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace FinanceTracker.Tests.Api;

public class SecurityHeadersTests
{
    private static HttpContext ContextFor(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        SecurityHeaders.Apply(context);
        return context;
    }

    [Theory]
    [InlineData("/api/Transactions")]
    [InlineData("/health/ready")]
    [InlineData("/swagger/index.html")]
    public void Apply_ShouldAddCommonHeaders_OnEveryPath(string path)
    {
        var headers = ContextFor(path).Response.Headers;

        Assert.Equal("max-age=31536000; includeSubDomains", headers["Strict-Transport-Security"]);
        Assert.Equal("nosniff", headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", headers["X-Frame-Options"]);
        Assert.Equal("no-referrer", headers["Referrer-Policy"]);
    }

    [Fact]
    public void Apply_ShouldLockDownApiResponses()
    {
        var headers = ContextFor("/api/Transactions").Response.Headers;

        Assert.Equal("default-src 'none'; frame-ancestors 'none'", headers["Content-Security-Policy"]);
        Assert.Equal("no-store", headers["Cache-Control"]);
    }

    [Fact]
    public void Apply_ShouldLeaveSwaggerUiWithoutCsp()
    {
        // Swagger UI necesita scripts inline: una CSP estricta lo romperia.
        var headers = ContextFor("/swagger/index.html").Response.Headers;

        Assert.False(headers.ContainsKey("Content-Security-Policy"));
        Assert.False(headers.ContainsKey("Cache-Control"));
    }
}
