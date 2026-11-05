using FinanceTracker.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace FinanceTracker.Tests.Services;

public class GoogleTokenValidatorTests
{
    private static GoogleTokenValidator ValidatorWith(Dictionary<string, string?> settings) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

    [Fact]
    public void Audiences_ShouldAcceptWebAndDesktopClientIds()
    {
        // El escritorio inicia sesion con su propio cliente de Google, asi que
        // su token trae como audiencia el client ID de escritorio, no el web.
        var validator = ValidatorWith(new()
        {
            ["Google:ClientId"] = "web-client-id",
            ["Google:DesktopClientId"] = "desktop-client-id"
        });

        Assert.Equal(["web-client-id", "desktop-client-id"], validator.Audiences());
    }

    [Fact]
    public void Audiences_ShouldOnlyHaveWebClientId_WhenNoDesktopClientIsConfigured()
    {
        var validator = ValidatorWith(new() { ["Google:ClientId"] = "web-client-id" });

        Assert.Equal(["web-client-id"], validator.Audiences());
    }
}
