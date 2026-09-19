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
    private static readonly (string Name, TransactionType Type)[] Definitions =
    {
        ("Nómina", TransactionType.Income),
        ("Otros ingresos", TransactionType.Income),
        ("Alquiler", TransactionType.Expense),
        ("Supermercado", TransactionType.Expense),
        ("Transporte", TransactionType.Expense),
        ("Suministros", TransactionType.Expense),
        ("Ocio", TransactionType.Expense),
        ("Otros gastos", TransactionType.Expense)
    };

    public static List<Category> ForUser(int userId)
    {
        return Definitions
            .Select(definition => new Category
            {
                Name = definition.Name,
                Type = definition.Type,
                UserId = userId
            })
            .ToList();
    }
}
