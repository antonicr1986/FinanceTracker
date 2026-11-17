using System.Security.Cryptography;
using System.Text;
using FinanceTracker.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Infrastructure.Services;

/// <summary>
/// Comprueba si una contraseña aparece en filtraciones conocidas con la API
/// Pwned Passwords de Have I Been Pwned (gratuita y sin clave).
///
/// k-anonimato: solo se envian los 5 primeros caracteres del SHA-1 de la
/// contraseña. La API devuelve todos los sufijos que empiezan asi (cientos) y
/// la comparacion se hace aqui, asi que ni la contraseña ni su hash completo
/// salen de la API. El SHA-1 es el formato que exige el servicio; no se usa
/// para guardar nada (eso es PasswordHasher).
/// </summary>
public class PwnedPasswordsChecker : IPasswordBreachChecker
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PwnedPasswordsChecker> _logger;

    public PwnedPasswordsChecker(HttpClient httpClient, ILogger<PwnedPasswordsChecker> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> IsBreachedAsync(string password)
    {
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
        var prefix = hash[..5];
        var suffix = hash[5..];

        try
        {
            var body = await _httpClient.GetStringAsync($"range/{prefix}");

            return ContainsSuffix(body, suffix);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // Si el servicio falla o tarda, no se bloquea el registro: es una
            // capa mas de proteccion, no la unica.
            _logger.LogWarning(exception, "Pwned Passwords is unavailable; skipping breach check.");

            return false;
        }
    }

    // Cada linea es "SUFIJO:VECES". Con la cabecera Add-Padding la respuesta
    // trae lineas de relleno con 0 veces, que no cuentan como filtradas.
    private static bool ContainsSuffix(string body, string suffix) =>
        body.Split('\n')
            .Select(line => line.Trim().Split(':'))
            .Any(parts => parts.Length == 2
                && parts[0].Equals(suffix, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(parts[1], out var count)
                && count > 0);
}
