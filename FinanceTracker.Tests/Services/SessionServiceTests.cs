using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Tests.Services;

public class SessionServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<User> AddUser(AppDbContext context)
    {
        var user = new User { Name = "Antonio", Email = "antonio@test.com", PasswordHash = "hashed-password" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task IsActiveAsync_ShouldAcceptAnyToken_WhenSessionsWereNeverRevoked()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var service = new SessionService(context);

        Assert.True(await service.IsActiveAsync(user.Id, DateTime.UnixEpoch));
    }

    [Fact]
    public async Task IsActiveAsync_ShouldRejectUnknownUsers()
    {
        using var context = CreateDbContext();
        var service = new SessionService(context);

        Assert.False(await service.IsActiveAsync(999, DateTime.UtcNow));
    }

    [Fact]
    public async Task RevokeAllAsync_ShouldRejectTokensIssuedBefore_AndAcceptNewOnes()
    {
        using var context = CreateDbContext();
        var user = await AddUser(context);
        var service = new SessionService(context);

        await service.RevokeAllAsync(user.Id);
        var revokedAt = user.SessionsRevokedAt!.Value;

        Assert.False(await service.IsActiveAsync(user.Id, revokedAt.AddSeconds(-1)));
        Assert.True(await service.IsActiveAsync(user.Id, revokedAt));
        Assert.True(await service.IsActiveAsync(user.Id, revokedAt.AddMinutes(1)));
    }

    [Fact]
    public async Task RevokeAllAsync_ShouldStoreWholeSeconds_LikeTheIatClaim()
    {
        // "iat" no lleva fracciones de segundo: si se guardaran, el token
        // emitido justo despues de revocar (mismo segundo) naceria invalido.
        using var context = CreateDbContext();
        var user = await AddUser(context);

        await new SessionService(context).RevokeAllAsync(user.Id);

        var revokedAt = user.SessionsRevokedAt!.Value;
        Assert.Equal(0, revokedAt.Ticks % TimeSpan.TicksPerSecond);
        Assert.InRange(revokedAt, DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow);
    }

    [Fact]
    public async Task RevokeAllAsync_ShouldNotRevokeTheDemoAccount()
    {
        // Compartida: cerrar sus sesiones echaria a todos los visitantes.
        using var context = CreateDbContext();
        await DemoDataSeeder.SeedAsync(context);
        var demo = await context.Users.SingleAsync(user => user.Email == DemoDataSeeder.DemoEmail);

        var revoked = await new SessionService(context).RevokeAllAsync(demo.Id);

        Assert.False(revoked);
        Assert.Null(demo.SessionsRevokedAt);
    }

    [Fact]
    public async Task RevokeAllAsync_ShouldOnlyAffectThatUser()
    {
        using var context = CreateDbContext();
        var antonio = await AddUser(context);
        var other = new User { Name = "Otra", Email = "otra@test.com", PasswordHash = "hashed-password" };
        context.Users.Add(other);
        await context.SaveChangesAsync();

        await new SessionService(context).RevokeAllAsync(antonio.Id);

        Assert.Null(other.SessionsRevokedAt);
    }
}
