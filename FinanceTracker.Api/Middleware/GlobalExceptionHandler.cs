using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Middleware
{
    /// <summary>
    /// Captura cualquier excepcion que no se haya controlado, la registra con
    /// el contexto de la peticion, y devuelve al cliente una respuesta uniforme.
    ///
    /// Sin esto, una excepcion sale como un 500 con el cuerpo vacio: el cliente
    /// no sabe que ha pasado y en el servidor no queda constancia de nada.
    ///
    /// Deliberadamente NO se devuelve el mensaje de la excepcion ni la traza:
    /// eso revelaria detalles internos (nombres de tablas, rutas, versiones) a
    /// cualquiera que provoque un error. El detalle va al log, no a la respuesta.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // Los valores entre llaves se guardan como propiedades consultables,
            // no solo como texto dentro del mensaje.
            _logger.LogError(
                exception,
                "Excepcion no controlada en {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);

            var problema = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Se ha producido un error inesperado.",
                Detail = "Consulte los registros del servidor para mas informacion.",
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode = problema.Status.Value;
            await httpContext.Response.WriteAsJsonAsync(problema, cancellationToken);

            return true;
        }
    }
}
