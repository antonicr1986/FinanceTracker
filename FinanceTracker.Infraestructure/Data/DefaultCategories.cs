using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Infrastructure.Data;

/// <summary>
/// Categorias que se crean junto con cada cuenta nueva.
///
/// Sin ellas, un usuario recien registrado no puede dar de alta ni un
/// movimiento: la API exige un CategoryId valido y suyo, de modo que el
/// formulario se encontraria un desplegable vacio. Se siembran al registrar en
/// lugar de obligarle a crearlas primero, que es pedir trabajo en el peor
/// momento posible.
///
/// No son inamovibles: son un punto de partida que el usuario puede renombrar o
/// ampliar con los endpoints de /api/Categories.
/// </summary>
public static class DefaultCategories
{
    // Cada categoria con su nombre en los dos idiomas de los clientes. Se
    // siembran en el idioma con el que se registra el usuario: alguien que usa
    // la aplicacion en ingles no deberia encontrarse "Supermercado".
    private static readonly (string Spanish, string English, TransactionType Type)[] Definitions =
    {
        ("Nómina", "Salary", TransactionType.Income),
        ("Otros ingresos", "Other income", TransactionType.Income),
        ("Alquiler", "Rent", TransactionType.Expense),
        ("Supermercado", "Groceries", TransactionType.Expense),
        ("Transporte", "Transport", TransactionType.Expense),
        ("Suministros", "Utilities", TransactionType.Expense),
        ("Ocio", "Leisure", TransactionType.Expense),
        ("Otros gastos", "Other expenses", TransactionType.Expense)
    };

    /// <summary>
    /// Ingles si el idioma empieza por "en" ("en", "EN", "en-GB"...); español
    /// en cualquier otro caso, incluido no indicar ninguno. Tolerante a
    /// proposito: rechazar un idioma desconocido con un 400 impediria
    /// registrarse por algo que no importa.
    /// </summary>
    public static bool IsEnglish(string? language) =>
        language is not null && language.Trim().StartsWith("en", StringComparison.OrdinalIgnoreCase);

    public static List<Category> ForUser(int userId, string? language = null)
    {
        var english = IsEnglish(language);

        return Definitions
            .Select(definition => new Category
            {
                Name = english ? definition.English : definition.Spanish,
                Type = definition.Type,
                UserId = userId
            })
            .ToList();
    }
}
