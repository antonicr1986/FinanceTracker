using FinanceTracker.Api.Controllers;
using FinanceTracker.Application.DTOs.Budgets;
using FinanceTracker.Application.DTOs.Categories;
using FinanceTracker.Application.DTOs.Dashboard;
using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Tests.Api;

/// <summary>
/// Controladores de categorias, movimientos, presupuestos, panel y sesiones con
/// los servicios reales y una base de datos en memoria: comprueban que cada
/// resultado del servicio acaba en la respuesta HTTP que esperan los clientes.
/// </summary>
public class CrudControllersTests
{
    private const int UserId = 1;

    private class FakeCurrentUser(int? userId) : ICurrentUserService
    {
        public int? UserId { get; } = userId;
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options);

    private static async Task<Category> AddCategory(AppDbContext context, TransactionType type, int userId = UserId)
    {
        var category = new Category { Name = type == TransactionType.Income ? "Nómina" : "Comida", Type = type, UserId = userId };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private static string? ProblemCode(IActionResult result) =>
        Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(result).Value).Extensions["code"] as string;

    private static string? ProblemCode<T>(ActionResult<T> result) => ProblemCode(result.Result!);

    // --- Categorias ---

    private static CategoriesController Categories(AppDbContext context) =>
        new(new CategoryService(context, new FakeCurrentUser(UserId)));

    [Fact]
    public async Task Categories_CreateGetUpdateDelete_RoundTrip()
    {
        using var context = CreateDbContext();
        var controller = Categories(context);

        var created = Assert.IsType<CreatedAtActionResult>((await controller.CreateCategory(
            new CreateCategoryDto { Name = "Gimnasio", Type = TransactionType.Expense })).Result);
        var category = Assert.IsType<CategoryDto>(created.Value);

        var found = Assert.IsType<OkObjectResult>((await controller.GetCategory(category.Id)).Result);
        Assert.Equal("Gimnasio", Assert.IsType<CategoryDto>(found.Value).Name);

        Assert.IsType<NoContentResult>(await controller.UpdateCategory(
            category.Id, new UpdateCategoryDto { Name = "Deporte", Type = TransactionType.Expense }));

        var all = Assert.IsType<OkObjectResult>((await controller.GetCategories()).Result);
        Assert.Contains(Assert.IsAssignableFrom<IEnumerable<CategoryDto>>(all.Value), c => c.Name == "Deporte");

        Assert.IsType<NoContentResult>(await controller.DeleteCategory(category.Id));
        Assert.IsType<NotFoundResult>((await controller.GetCategory(category.Id)).Result);
    }

    [Fact]
    public async Task Categories_UnknownOrForeign_AreNotFound()
    {
        using var context = CreateDbContext();
        var foreign = await AddCategory(context, TransactionType.Expense, userId: 2);
        var controller = Categories(context);

        Assert.IsType<NotFoundResult>((await controller.GetCategory(foreign.Id)).Result);
        Assert.IsType<NotFoundResult>(await controller.UpdateCategory(
            foreign.Id, new UpdateCategoryDto { Name = "x", Type = TransactionType.Expense }));
        Assert.IsType<NotFoundResult>(await controller.DeleteCategory(999));
    }

    [Fact]
    public async Task Categories_WithTransactions_CannotBeDeleted()
    {
        using var context = CreateDbContext();
        var category = await AddCategory(context, TransactionType.Expense);
        context.Transactions.Add(new Transaction
        {
            Description = "Pan",
            Amount = 2m,
            Date = DateTime.UtcNow,
            Type = TransactionType.Expense,
            CategoryId = category.Id,
            UserId = UserId
        });
        await context.SaveChangesAsync();

        Assert.Equal("category_has_transactions", ProblemCode(await Categories(context).DeleteCategory(category.Id)));
    }

    // --- Movimientos ---

    private static TransactionsController Transactions(AppDbContext context) =>
        new(new TransactionService(context, new FakeCurrentUser(UserId)));

    private static CreateTransactionDto NewTransaction(int categoryId, TransactionType type = TransactionType.Expense) =>
        new() { Description = "Compra", Amount = 25m, Date = new DateTime(2026, 11, 1), Type = type, CategoryId = categoryId };

    [Fact]
    public async Task Transactions_CreateGetUpdateDelete_RoundTrip()
    {
        using var context = CreateDbContext();
        var category = await AddCategory(context, TransactionType.Expense);
        var controller = Transactions(context);

        var created = Assert.IsType<CreatedAtActionResult>((await controller.CreateTransaction(NewTransaction(category.Id))).Result);
        var transaction = Assert.IsType<TransactionDto>(created.Value);

        Assert.IsType<OkObjectResult>((await controller.GetTransaction(transaction.Id)).Result);
        Assert.IsType<OkObjectResult>((await controller.GetTransactions(new TransactionFilterDto())).Result);

        var summary = Assert.IsType<OkObjectResult>((await controller.GetSummary(new TransactionFilterDto())).Result);
        Assert.Equal(25m, Assert.IsType<TransactionSummaryDto>(summary.Value).TotalExpense);

        Assert.IsType<NoContentResult>(await controller.UpdateTransaction(transaction.Id, new UpdateTransactionDto
        {
            Description = "Compra grande",
            Amount = 40m,
            Date = new DateTime(2026, 11, 2),
            Type = TransactionType.Expense,
            CategoryId = category.Id
        }));

        Assert.IsType<NoContentResult>(await controller.DeleteTransaction(transaction.Id));
        Assert.IsType<NotFoundResult>((await controller.GetTransaction(transaction.Id)).Result);
        Assert.IsType<NotFoundResult>(await controller.DeleteTransaction(transaction.Id));
    }

    [Fact]
    public async Task Transactions_WithAWrongCategory_AreBadRequests()
    {
        using var context = CreateDbContext();
        var income = await AddCategory(context, TransactionType.Income);
        var controller = Transactions(context);

        Assert.Equal("category_not_found", ProblemCode(await controller.CreateTransaction(NewTransaction(999))));
        Assert.Equal("category_type_mismatch", ProblemCode(await controller.CreateTransaction(NewTransaction(income.Id))));
    }

    [Fact]
    public async Task Transactions_UpdateErrors_MapToTheirResponses()
    {
        using var context = CreateDbContext();
        var expense = await AddCategory(context, TransactionType.Expense);
        var income = await AddCategory(context, TransactionType.Income);
        var controller = Transactions(context);
        var created = Assert.IsType<CreatedAtActionResult>((await controller.CreateTransaction(NewTransaction(expense.Id))).Result);
        var id = Assert.IsType<TransactionDto>(created.Value).Id;

        UpdateTransactionDto Update(int categoryId) => new()
        {
            Description = "Compra",
            Amount = 10m,
            Date = new DateTime(2026, 11, 3),
            Type = TransactionType.Expense,
            CategoryId = categoryId
        };

        Assert.IsType<NotFoundResult>(await controller.UpdateTransaction(999, Update(expense.Id)));
        Assert.Equal("category_not_found", ProblemCode(await controller.UpdateTransaction(id, Update(999))));
        Assert.Equal("category_type_mismatch", ProblemCode(await controller.UpdateTransaction(id, Update(income.Id))));
    }

    // --- Presupuestos ---

    private static BudgetsController Budgets(AppDbContext context) =>
        new(new BudgetService(context, new FakeCurrentUser(UserId)));

    private static CreateBudgetDto NewBudget(int? categoryId = null, TransactionType type = TransactionType.Expense) =>
        new() { Name = "Comida", Amount = 300m, Month = 11, Year = 2026, Type = type, CategoryId = categoryId };

    private static UpdateBudgetDto BudgetUpdate(int? categoryId = null) =>
        new() { Name = "Comida", Amount = 350m, Month = 11, Year = 2026, Type = TransactionType.Expense, CategoryId = categoryId };

    [Fact]
    public async Task Budgets_CreateGetUpdateDelete_RoundTrip()
    {
        using var context = CreateDbContext();
        var category = await AddCategory(context, TransactionType.Expense);
        var controller = Budgets(context);

        var created = Assert.IsType<CreatedAtActionResult>((await controller.CreateBudget(NewBudget(category.Id))).Result);
        var budget = Assert.IsType<BudgetDto>(created.Value);

        Assert.IsType<OkObjectResult>((await controller.GetBudget(budget.Id)).Result);
        Assert.IsType<OkObjectResult>((await controller.GetBudgets()).Result);
        Assert.IsType<NoContentResult>(await controller.UpdateBudget(budget.Id, BudgetUpdate(category.Id)));
        Assert.IsType<NoContentResult>(await controller.DeleteBudget(budget.Id));
        Assert.IsType<NotFoundResult>((await controller.GetBudget(budget.Id)).Result);
        Assert.IsType<NotFoundResult>(await controller.DeleteBudget(budget.Id));
    }

    [Fact]
    public async Task Budgets_WithAWrongCategory_AreBadRequests()
    {
        using var context = CreateDbContext();
        var income = await AddCategory(context, TransactionType.Income);
        var controller = Budgets(context);

        Assert.Equal("category_not_found", ProblemCode(await controller.CreateBudget(NewBudget(999))));
        Assert.Equal("category_type_mismatch", ProblemCode(await controller.CreateBudget(NewBudget(income.Id))));

        var created = Assert.IsType<CreatedAtActionResult>((await controller.CreateBudget(NewBudget())).Result);
        var id = Assert.IsType<BudgetDto>(created.Value).Id;

        Assert.IsType<NotFoundResult>(await controller.UpdateBudget(999, BudgetUpdate()));
        Assert.Equal("category_not_found", ProblemCode(await controller.UpdateBudget(id, BudgetUpdate(999))));
        Assert.Equal("category_type_mismatch", ProblemCode(await controller.UpdateBudget(id, BudgetUpdate(income.Id))));
    }

    // --- Panel y sesiones ---

    [Fact]
    public async Task Dashboard_ReturnsTheSummary()
    {
        using var context = CreateDbContext();
        var category = await AddCategory(context, TransactionType.Expense);
        await Transactions(context).CreateTransaction(NewTransaction(category.Id));
        var controller = new DashboardController(new DashboardService(context));

        var ok = Assert.IsType<OkObjectResult>((await controller.GetSummary(new TransactionFilterDto())).Result);

        Assert.IsType<DashboardSummaryDto>(ok.Value);
    }

    [Fact]
    public async Task Sessions_RevokeAll_StampsTheUser()
    {
        using var context = CreateDbContext();
        var user = new User { Name = "Ana", Email = "ana@test.com", PasswordHash = "hash" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var controller = new SessionsController(new SessionService(context), new FakeCurrentUser(user.Id));

        Assert.IsType<NoContentResult>(await controller.RevokeAll());
        Assert.NotNull(user.SessionsRevokedAt);
    }

    [Fact]
    public async Task Sessions_RevokeAll_WithoutAUser_IsUnauthorized()
    {
        using var context = CreateDbContext();
        var controller = new SessionsController(new SessionService(context), new FakeCurrentUser(null));

        Assert.IsType<UnauthorizedResult>(await controller.RevokeAll());
    }
}
