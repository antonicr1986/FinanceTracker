using FinanceTracker.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FinanceTracker.Tests.Api;

public class ClientVersionTests
{
    private static IConfiguration MinimumDesktop(string version) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ClientVersions:Minimum:desktop"] = version,
            })
            .Build();

    private static DefaultHttpContext Request(string path, string? platform, string? version)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        if (platform is not null)
        {
            context.Request.Headers["X-Client-Platform"] = platform;
        }

        if (version is not null)
        {
            context.Request.Headers["X-Client-Version"] = version;
        }

        return context;
    }

    private static async Task<string> BodyOf(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await new StreamReader(context.Response.Body).ReadToEndAsync();
    }

    [Fact]
    public async Task OlderVersion_ShouldReturn426_WithMinimumVersion()
    {
        // Arrange
        var context = Request("/api/Transactions", "desktop", "1.11.6");
        var nextCalled = false;

        // Act
        await ClientVersionCheck.InvokeAsync(context, MinimumDesktop("1.12.0"), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status426UpgradeRequired, context.Response.StatusCode);
        var body = await BodyOf(context);
        Assert.Contains("\"code\":\"client_version_unsupported\"", body);
        Assert.Contains("\"minimumVersion\":\"1.12.0\"", body);
    }

    [Fact]
    public async Task SameVersion_ShouldPass()
    {
        // Arrange
        var context = Request("/api/Transactions", "desktop", "1.12.0");
        var nextCalled = false;

        // Act
        await ClientVersionCheck.InvokeAsync(context, MinimumDesktop("1.12.0"), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task NewerVersion_ShouldPass()
    {
        // Arrange
        var context = Request("/api/Transactions", "desktop", "1.13.0");
        var nextCalled = false;

        // Act
        await ClientVersionCheck.InvokeAsync(context, MinimumDesktop("1.12.0"), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task MissingHeaders_ShouldPass()
    {
        // Arrange: una version ya instalada que todavia no envia las cabeceras
        var context = Request("/api/Transactions", platform: null, version: null);
        var nextCalled = false;

        // Act
        await ClientVersionCheck.InvokeAsync(context, MinimumDesktop("1.12.0"), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task PlatformWithoutMinimum_ShouldPass()
    {
        // Arrange: solo hay minima para desktop
        var context = Request("/api/Transactions", "android", "0.1.0");
        var nextCalled = false;

        // Act
        await ClientVersionCheck.InvokeAsync(context, MinimumDesktop("1.12.0"), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/swagger/index.html")]
    public async Task HealthAndSwagger_ShouldPass(string path)
    {
        // Arrange
        var context = Request(path, "desktop", "1.11.6");
        var nextCalled = false;

        // Act
        await ClientVersionCheck.InvokeAsync(context, MinimumDesktop("1.12.0"), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task UnreadableVersion_ShouldPass()
    {
        // Arrange
        var context = Request("/api/Transactions", "desktop", "abc");
        var nextCalled = false;

        // Act
        await ClientVersionCheck.InvokeAsync(context, MinimumDesktop("1.12.0"), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }
}
