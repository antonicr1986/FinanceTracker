using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace FinanceTracker.Infrastructure.Services;

/// <summary>
/// Envoltorio fino de la libreria de Google: comprueba la firma, la caducidad
/// y que el token se emitio para nuestro client ID web. Sin tests unitarios
/// porque necesita tokens firmados por Google; se verifica a mano.
/// </summary>
public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly IConfiguration _configuration;

    public GoogleTokenValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GoogleIdentity?> ValidateAsync(string idToken)
    {
        // Sin audiencia la libreria no la comprobaria y aceptaria tokens
        // emitidos para cualquier otra app: mejor fallar.
        var clientId = _configuration["Google:ClientId"]
            ?? throw new InvalidOperationException("Google client ID is not configured.");

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });

            return new GoogleIdentity(payload.Subject, payload.Email, payload.EmailVerified, payload.Name ?? payload.Email);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
