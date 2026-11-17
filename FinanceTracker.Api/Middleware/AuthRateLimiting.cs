using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FinanceTracker.Api.Middleware;

/// <summary>
/// Limite de peticiones a los endpoints de autenticacion (registro, login y
/// Google), por IP de cliente. Frena la fuerza bruta de contraseñas y el alta
/// masiva de cuentas. Se aplica con [EnableRateLimiting(AuthRateLimiting.PolicyName)].
/// </summary>
public static class AuthRateLimiting
{
    public const string PolicyName = "auth";

    // 10 por minuto y por IP: de sobra para quien se equivoca al escribir, muy
    // poco para probar contraseñas a ciegas. Sin cola: lo que sobra se rechaza.
    public const int PermitLimit = 10;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.AddPolicy<string>(PolicyName, Partition);
            options.OnRejected = WriteRejectionAsync;
        });
    }

    /// <summary>
    /// Un contador por IP. La IP es la del cliente real gracias a
    /// UseForwardedHeaders (ver Program.cs); sin eso, detras de Azure todas las
    /// peticiones llegarian con la IP del balanceador y compartirian contador.
    /// </summary>
    public static RateLimitPartition<string> Partition(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = PermitLimit,
            Window = Window,
            QueueLimit = 0
        });
    }

    // 429 con el mismo formato que el resto de errores (ver ApiProblems), para
    // que los clientes puedan traducir el codigo.
    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "too_many_requests",
            Detail = "Too many attempts. Please try again in a minute."
        };

        problem.Extensions["code"] = "too_many_requests";

        await response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    }
}
