using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Data;

/// <summary>
/// Crea la cuenta de demostracion y sus datos de ejemplo si no existen todavia.
/// Sirve para que cualquiera pueda entrar en la aplicacion desplegada y ver
/// algo real sin tener que registrarse. Es idempotente: si la cuenta ya esta
/// creada no toca nada, asi que se puede ejecutar en cada arranque.
/// </summary>
public static class DemoDataSeeder
{
    public const string DemoEmail = "demo@financetracker.app";
    // Credenciales publicas a proposito: cuenta de demostracion abierta.
    public const string DemoPassword = "Demo1234!"; // gitleaks:allow
    private const string DemoName = "Usuario demo";

    public static async Task SeedAsync(AppDbContext context)
    {
        var alreadySeeded = await context.Users
            .AnyAsync(user => user.Email == DemoEmail);

        if (alreadySeeded)
        {
            return;
        }

        var user = new User
        {
            Name = DemoName,
            Email = DemoEmail
        };

        user.PasswordHash = new PasswordHasher<User>()
            .HashPassword(user, DemoPassword); // gitleaks:allow

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var categories = new Dictionary<string, Category>
        {
            ["Nomina"] = new() { Name = "Nomina", Type = TransactionType.Income, UserId = user.Id },
            ["Freelance"] = new() { Name = "Freelance", Type = TransactionType.Income, UserId = user.Id },
            ["Alquiler"] = new() { Name = "Alquiler", Type = TransactionType.Expense, UserId = user.Id },
            ["Supermercado"] = new() { Name = "Supermercado", Type = TransactionType.Expense, UserId = user.Id },
            ["Transporte"] = new() { Name = "Transporte", Type = TransactionType.Expense, UserId = user.Id },
            ["Suministros"] = new() { Name = "Suministros", Type = TransactionType.Expense, UserId = user.Id },
            ["Ocio"] = new() { Name = "Ocio", Type = TransactionType.Expense, UserId = user.Id }
        };

        context.Categories.AddRange(categories.Values);
        await context.SaveChangesAsync();

        // Seis meses de movimientos, del mas antiguo al mes actual. Las cifras
        // son fijas a proposito: asi la demo se ve igual en cada despliegue.
        var freelanceByMonth = new[] { 0m, 450m, 0m, 620m, 0m, 380m };
        var groceriesByMonth = new[] { 312.40m, 358.15m, 291.80m, 402.55m, 334.90m, 368.20m };
        var leisureByMonth = new[] { 74.50m, 120.00m, 48.90m, 155.30m, 96.75m, 132.40m };

        var transactions = new List<Transaction>();
        var firstMonth = DateTime.UtcNow.Date.AddMonths(-5);
        var startMonth = new DateTime(firstMonth.Year, firstMonth.Month, 1);

        for (var offset = 0; offset < 6; offset++)
        {
            var month = startMonth.AddMonths(offset);

            transactions.Add(Movement(user.Id, categories["Nomina"], TransactionType.Income,
                "Nomina mensual", 1900m, month.AddDays(0)));

            if (freelanceByMonth[offset] > 0)
            {
                transactions.Add(Movement(user.Id, categories["Freelance"], TransactionType.Income,
                    "Proyecto freelance", freelanceByMonth[offset], month.AddDays(17)));
            }

            transactions.Add(Movement(user.Id, categories["Alquiler"], TransactionType.Expense,
                "Alquiler", 750m, month.AddDays(2)));

            transactions.Add(Movement(user.Id, categories["Supermercado"], TransactionType.Expense,
                "Compra del mes", groceriesByMonth[offset], month.AddDays(8)));

            transactions.Add(Movement(user.Id, categories["Transporte"], TransactionType.Expense,
                "Abono transporte", 54.60m, month.AddDays(4)));

            transactions.Add(Movement(user.Id, categories["Suministros"], TransactionType.Expense,
                "Luz, agua e internet", 128.35m, month.AddDays(12)));

            transactions.Add(Movement(user.Id, categories["Ocio"], TransactionType.Expense,
                "Ocio y restaurantes", leisureByMonth[offset], month.AddDays(21)));
        }

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();
    }

    private static Transaction Movement(
        int userId,
        Category category,
        TransactionType type,
        string description,
        decimal amount,
        DateTime date)
    {
        return new Transaction
        {
            UserId = userId,
            CategoryId = category.Id,
            Type = type,
            Description = description,
            Amount = amount,
            Date = date
        };
    }
}
