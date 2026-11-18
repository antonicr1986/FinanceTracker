using FinanceTracker.Application.DTOs.Mfa;
using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinanceTracker.Infrastructure.Services;

public class TokenService : ITokenService
{
    // Minutos para escribir el codigo del MFA tras la contraseña.
    public const int MfaChallengeMinutes = 5;

    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public LoginResponseDto CreateToken(UserDto user)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key is not configured.");

        var jwtIssuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT issuer is not configured.");

        var jwtAudience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT audience is not configured.");

        var expirationMinutes = int.Parse(
            _configuration["Jwt:ExpirationMinutes"] ?? "60");

        var now = DateTime.UtcNow;
        var expiration = now.AddMinutes(expirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            // Fecha de emision: la API la compara con SessionsRevokedAt para
            // rechazar los tokens de antes de "cerrar sesion en todos".
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials);

        return new LoginResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Expiration = expiration,
            User = user
        };
    }

    /// <summary>
    /// Un JWT corto con otra audiencia ("...:mfa"). La API solo acepta como
    /// sesion los tokens con la audiencia normal, asi que este no sirve para
    /// llamar a ningun endpoint: solo para /api/users/mfa/verify.
    /// </summary>
    public MfaChallengeDto CreateMfaChallenge(int userId)
    {
        var (key, issuer, audience) = ReadJwtSettings();
        var expiration = DateTime.UtcNow.AddMinutes(MfaChallengeMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: MfaAudience(audience),
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            },
            expires: expiration,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new MfaChallengeDto
        {
            MfaToken = new JwtSecurityTokenHandler().WriteToken(token),
            Expiration = expiration
        };
    }

    public int? ValidateMfaChallenge(string mfaToken)
    {
        var (key, issuer, audience) = ReadJwtSettings();

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = MfaAudience(audience),
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(mfaToken, parameters, out _);

            return int.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId)
                ? userId
                : null;
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }

    private static string MfaAudience(string audience) => $"{audience}:mfa";

    private (SymmetricSecurityKey Key, string Issuer, string Audience) ReadJwtSettings()
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key is not configured.");

        var jwtIssuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT issuer is not configured.");

        var jwtAudience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT audience is not configured.");

        return (new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), jwtIssuer, jwtAudience);
    }
}
