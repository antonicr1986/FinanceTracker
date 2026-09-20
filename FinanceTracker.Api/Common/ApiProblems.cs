using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Common;

/// <summary>
/// Respuestas de error con codigo y explicacion.
///
/// El codigo es para el cliente: identifica el caso sin depender del idioma, de
/// modo que la aplicacion web puede traducirlo a lo que corresponda. La frase
/// es para quien prueba la API a mano desde Swagger, que con un codigo suelto
/// se quedaria a medias.
///
/// Van en un ProblemDetails, que es el formato estandar de errores en HTTP, con
/// el codigo tambien como propiedad de primer nivel ("code") para que el cliente
/// no tenga que adivinar donde mirar.
/// </summary>
public static class ApiProblems
{
    public static ObjectResult Of(int status, string code, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = code,
            Detail = detail
        };

        problem.Extensions["code"] = code;

        return new ObjectResult(problem) { StatusCode = status };
    }

    public static ObjectResult BadRequest(string code, string detail) =>
        Of(StatusCodes.Status400BadRequest, code, detail);

    public static ObjectResult Unauthorized(string code, string detail) =>
        Of(StatusCodes.Status401Unauthorized, code, detail);
}
