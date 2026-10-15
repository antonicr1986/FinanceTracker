namespace FinanceTracker.Api.Middleware;

/// <summary>
/// Cabeceras de seguridad en todas las respuestas, a partir del informe de
/// OWASP ZAP (job dast del CI).
/// </summary>
public static class SecurityHeaders
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            Apply(context);
            await next(context);
        });
    }

    public static void Apply(HttpContext context)
    {
        var headers = context.Response.Headers;
        var isSwagger = context.Request.Path.StartsWithSegments("/swagger");

        // HSTS se pone a mano: el TLS lo termina Azure y dentro del contenedor
        // la peticion llega por HTTP, asi que UseHsts() no la anadiria nunca.
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";

        if (!isSwagger)
        {
            // Las respuestas de la API son JSON, nunca una pagina: no necesitan
            // cargar nada. Swagger UI se queda fuera porque usa scripts inline.
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

            // Son datos de cada usuario: que ninguna cache intermedia los guarde.
            headers["Cache-Control"] = "no-store";

            // Ninguna otra web puede incrustar estas respuestas (img, script...).
            // No afecta a los clientes: la web usa fetch con CORS, que no la
            // comprueba, y las apps de Android y Windows no son navegadores.
            headers["Cross-Origin-Resource-Policy"] = "same-origin";
        }
    }
}
