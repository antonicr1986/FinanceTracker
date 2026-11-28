using FinanceTracker.Api.Controllers;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Tests.Api;

public class SessionsControllerTests
{
    private class FakeCurrentUser(int? userId) : ICurrentUserService
    {
        public int? UserId { get; } = userId;
    }

    [Fact]
    public async Task RevokeAll_ShouldBeForbidden_ForTheDemoAccount()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        await DemoDataSeeder.SeedAsync(context);
        var demo = await context.Users.SingleAsync(user => user.Email == DemoDataSeeder.DemoEmail);
        var controller = new SessionsController(new SessionService(context), new FakeCurrentUser(demo.Id));

        var result = await controller.RevokeAll();

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, problem.StatusCode);
        Assert.Equal("demo_account", Assert.IsType<ProblemDetails>(problem.Value).Extensions["code"]);
    }
}
