using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Middleware;

/// <summary>
/// Version minima de cliente por plataforma (issue #22). Las apps envian
/// X-Client-Platform y X-Client-Version; si la version es anterior a la minima
/// configurada en ClientVersions:Minimum:{plataforma}, se responde 426 para que
/// la app pida actualizar en vez de fallar de forma rara.
/// </summary>
public static class ClientVersionCheck
{
    public static async Task InvokeAsync(HttpContext context, IConfiguration configuration, RequestDelegate next)
    {
        var platform = context.Request.Headers["X-Client-Platform"].ToString();
        var clientVersion = context.Request.Headers["X-Client-Version"].ToString();

        // Solo se corta si todo esta claro: hay cabeceras (las versiones ya
        // instaladas no las envian), la plataforma tiene minima configurada y
        // las dos versiones se pueden leer. Las comprobaciones de salud de
        // Azure y Swagger no son una app: nunca se cortan.
        var isApp = !context.Request.Path.StartsWithSegments("/health")
            && !context.Request.Path.StartsWithSegments("/swagger");

        if (isApp
            && platform.Length > 0
            && Version.TryParse(configuration[$"ClientVersions:Minimum:{platform}"], out var minimum)
            && Version.TryParse(clientVersion, out var version)
            && version < minimum)
        {
            context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status426UpgradeRequired,
                    Title = "Client version not supported",
                    Detail = $"Update the app to version {minimum} or later.",
                    Extensions = { ["minimumVersion"] = minimum.ToString() },
                },
                options: null,
                contentType: "application/problem+json");
            return;
        }

        await next(context);
    }
}
