using System.IdentityModel.Tokens.Jwt;
using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace FinanceTracker.Tests.Services;

public class TokenServiceTests
{
    private static TokenService CreateService() => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "clave-solo-para-tests-clave-solo-para-tests-1234", // gitleaks:allow
            ["Jwt:Issuer"] = "FinanceTracker.Api",
            ["Jwt:Audience"] = "FinanceTracker.Client"
        })
        .Build());

    [Fact]
    public void ValidateMfaChallenge_ShouldReturnTheUserId_ForAChallengeItCreated()
    {
        var service = CreateService();

        var challenge = service.CreateMfaChallenge(42);

        Assert.True(challenge.MfaRequired);
        Assert.Equal(42, service.ValidateMfaChallenge(challenge.MfaToken));
    }

    [Fact]
    public void MfaChallenge_ShouldNotUseTheSessionAudience()
    {
        // La API solo acepta como sesion la audiencia normal: con otra, este
        // token no abre ningun endpoint aunque este bien firmado.
        var challenge = CreateService().CreateMfaChallenge(42);

        var audience = new JwtSecurityTokenHandler().ReadJwtToken(challenge.MfaToken).Audiences.Single();

        Assert.NotEqual("FinanceTracker.Client", audience);
    }

    [Fact]
    public void ValidateMfaChallenge_ShouldRejectASessionToken()
    {
        var service = CreateService();
        var session = service.CreateToken(new UserDto { Id = 42, Name = "Antonio", Email = "antonio@test.com" });

        Assert.Null(service.ValidateMfaChallenge(session.Token));
    }

    [Fact]
    public void CreateToken_ShouldIncludeTheIssuedAtClaim()
    {
        // Sin "iat" la API no puede saber si el token es anterior a "cerrar
        // sesion en todos los dispositivos".
        var session = CreateService().CreateToken(new UserDto { Id = 42, Name = "Antonio", Email = "antonio@test.com" });

        var token = new JwtSecurityTokenHandler().ReadJwtToken(session.Token);
        var iat = long.Parse(token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Iat).Value);

        Assert.InRange(
            DateTimeOffset.FromUnixTimeSeconds(iat).UtcDateTime,
            DateTime.UtcNow.AddSeconds(-5),
            DateTime.UtcNow.AddSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-es-un-jwt")]
    public void ValidateMfaChallenge_ShouldRejectGarbage(string mfaToken)
    {
        Assert.Null(CreateService().ValidateMfaChallenge(mfaToken));
    }
}
