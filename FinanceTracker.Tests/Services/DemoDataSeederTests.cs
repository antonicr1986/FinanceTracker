using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Tests.Services;

public class DemoDataSeederTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static readonly string[] DemoCategories =
        ["Nomina", "Freelance", "Alquiler", "Supermercado", "Transporte", "Suministros", "Ocio"];

    [Fact]
    public async Task ResetAsync_ShouldRestoreTheDemoData_AfterVisitorsChangeIt()
    {
        using var context = CreateDbContext();
        await DemoDataSeeder.SeedAsync(context);
        var demo = await context.Users.SingleAsync(user => user.Email == DemoDataSeeder.DemoEmail);
        var seededCount = await context.Transactions.CountAsync(t => t.UserId == demo.Id);

        // Lo que podria hacer un visitante.
        context.Transactions.RemoveRange(context.Transactions.Where(t => t.UserId == demo.Id).Take(3));
        var category = await context.Categories.FirstAsync(c => c.UserId == demo.Id);
        context.Transactions.Add(new Transaction
        {
            UserId = demo.Id,
            CategoryId = category.Id,
            Type = TransactionType.Expense,
            Description = "texto ofensivo",
            Amount = 1,
            Date = DateTime.UtcNow
        });
        category.Name = "otro texto ofensivo";
        context.Budgets.Add(new Budget { UserId = demo.Id, Name = "vandalismo", Amount = 1, Month = 1, Year = 2026 });
        demo.Name = "Hackeado";
        await context.SaveChangesAsync();

        await DemoDataSeeder.ResetAsync(context);

        var transactions = await context.Transactions.Where(t => t.UserId == demo.Id).ToListAsync();
        Assert.Equal(seededCount, transactions.Count);
        Assert.DoesNotContain(transactions, t => t.Description == "texto ofensivo");
        Assert.Equal(DemoCategories.Order(),
            (await context.Categories.Where(c => c.UserId == demo.Id).Select(c => c.Name).ToListAsync()).Order());
        Assert.False(await context.Budgets.AnyAsync(b => b.UserId == demo.Id));
        Assert.Equal("Usuario demo", demo.Name);
        // La misma cuenta (mismo id): las sesiones abiertas siguen valiendo.
        Assert.Equal(1, await context.Users.CountAsync(user => user.Email == DemoDataSeeder.DemoEmail));
    }

    [Fact]
    public async Task ResetAsync_ShouldNotTouchOtherUsers()
    {
        using var context = CreateDbContext();
        await DemoDataSeeder.SeedAsync(context);
        var other = new User { Name = "Antonio", Email = "antonio@test.com", PasswordHash = "hash" };
        context.Users.Add(other);
        await context.SaveChangesAsync();
        var category = new Category { Name = "Casa", Type = TransactionType.Expense, UserId = other.Id };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        context.Transactions.Add(new Transaction
        {
            UserId = other.Id,
            CategoryId = category.Id,
            Type = TransactionType.Expense,
            Description = "Mio",
            Amount = 10,
            Date = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        await DemoDataSeeder.ResetAsync(context);

        Assert.True(await context.Transactions.AnyAsync(t => t.UserId == other.Id && t.Description == "Mio"));
        Assert.True(await context.Categories.AnyAsync(c => c.UserId == other.Id && c.Name == "Casa"));
    }

    [Fact]
    public async Task ResetAsync_ShouldCreateTheDemo_WhenItDoesNotExist()
    {
        using var context = CreateDbContext();

        await DemoDataSeeder.ResetAsync(context);

        Assert.True(await context.Users.AnyAsync(user => user.Email == DemoDataSeeder.DemoEmail));
    }

    [Theory]
    [InlineData("2026-11-28T01:00:00", 2.0)]   // misma noche: dentro de 2 horas
    [InlineData("2026-11-28T03:00:00", 24.0)]  // justo a la hora: el de mañana
    [InlineData("2026-11-28T04:30:00", 22.5)]  // ya pasado: el de mañana
    public void DelayUntilNextReset_ShouldWaitUntil3amUtc(string nowUtc, double expectedHours)
    {
        var now = DateTime.SpecifyKind(DateTime.Parse(nowUtc), DateTimeKind.Utc);

        Assert.Equal(TimeSpan.FromHours(expectedHours), DemoResetSchedule.DelayUntilNextReset(now));
    }
}
