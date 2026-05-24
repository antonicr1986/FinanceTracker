using FinanceTracker.Application.Common;
using FinanceTracker.Application.DTOs.Budgets;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Tests.Services;

public class BudgetServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateBudget_WhenCategoryExists()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Type = TransactionType.Expense
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = new BudgetService(context);

        var createBudgetDto = new CreateBudgetDto
        {
            Name = "Food budget May",
            Amount = 300m,
            Month = 5,
            Year = 2026,
            Type = TransactionType.Expense,
            CategoryId = 1
        };

        // Act
        var result = await service.CreateAsync(createBudgetDto);

        // Assert
        Assert.Equal(BudgetOperationResult.Success, result.Result);
        Assert.NotNull(result.Budget);
        Assert.Equal("Food budget May", result.Budget.Name);
        Assert.Equal(300m, result.Budget.Amount);
        Assert.Equal(5, result.Budget.Month);
        Assert.Equal(2026, result.Budget.Year);
        Assert.Equal(TransactionType.Expense, result.Budget.Type);
        Assert.Equal(1, result.Budget.CategoryId);
        Assert.Equal("Food", result.Budget.CategoryName);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnNull_WhenCategoryDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();

        var service = new BudgetService(context);

        var createBudgetDto = new CreateBudgetDto
        {
            Name = "Invalid budget",
            Amount = 300m,
            Month = 5,
            Year = 2026,
            Type = TransactionType.Expense,
            CategoryId = 999
        };

        // Act
        var result = await service.CreateAsync(createBudgetDto);

        // Assert
        Assert.Equal(BudgetOperationResult.CategoryNotFound, result.Result);
        Assert.Null(result.Budget);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllBudgets()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Type = TransactionType.Expense
        };

        context.Categories.Add(category);

        context.Budgets.AddRange(
            new Budget
            {
                Name = "Food budget May",
                Amount = 300m,
                Month = 5,
                Year = 2026,
                Type = TransactionType.Expense,
                CategoryId = 1
            },
            new Budget
            {
                Name = "Food budget June",
                Amount = 350m,
                Month = 6,
                Year = 2026,
                Type = TransactionType.Expense,
                CategoryId = 1
            }
        );

        await context.SaveChangesAsync();

        var service = new BudgetService(context);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Food budget June", result[0].Name);
        Assert.Equal("Food budget May", result[1].Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnBudget_WhenBudgetExists()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Type = TransactionType.Expense
        };

        context.Categories.Add(category);

        var budget = new Budget
        {
            Name = "Food budget May",
            Amount = 300m,
            Month = 5,
            Year = 2026,
            Type = TransactionType.Expense,
            CategoryId = 1
        };

        context.Budgets.Add(budget);
        await context.SaveChangesAsync();

        var service = new BudgetService(context);

        // Act
        var result = await service.GetByIdAsync(budget.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Food budget May", result.Name);
        Assert.Equal(300m, result.Amount);
        Assert.Equal("Food", result.CategoryName);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateBudget_WhenBudgetExists()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Type = TransactionType.Expense
        };

        context.Categories.Add(category);

        var budget = new Budget
        {
            Name = "Food budget May",
            Amount = 300m,
            Month = 5,
            Year = 2026,
            Type = TransactionType.Expense,
            CategoryId = 1
        };

        context.Budgets.Add(budget);
        await context.SaveChangesAsync();

        var service = new BudgetService(context);

        var updateBudgetDto = new UpdateBudgetDto
        {
            Name = "Food budget May updated",
            Amount = 350m,
            Month = 5,
            Year = 2026,
            Type = TransactionType.Expense,
            CategoryId = 1
        };

        // Act
        var result = await service.UpdateAsync(budget.Id, updateBudgetDto);

        // Assert
        Assert.Equal(BudgetOperationResult.Success, result);

        var updatedBudget = await context.Budgets.FindAsync(budget.Id);

        Assert.NotNull(updatedBudget);
        Assert.Equal("Food budget May updated", updatedBudget.Name);
        Assert.Equal(350m, updatedBudget.Amount);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteBudget_WhenBudgetExists()
    {
        // Arrange
        using var context = CreateDbContext();

        var budget = new Budget
        {
            Name = "Food budget May",
            Amount = 300m,
            Month = 5,
            Year = 2026,
            Type = TransactionType.Expense,
            CategoryId = null
        };

        context.Budgets.Add(budget);
        await context.SaveChangesAsync();

        var service = new BudgetService(context);

        // Act
        var result = await service.DeleteAsync(budget.Id);

        // Assert
        Assert.True(result);

        var deletedBudget = await context.Budgets.FindAsync(budget.Id);
        Assert.Null(deletedBudget);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnCategoryTypeMismatch_WhenCategoryTypeDoesNotMatchBudgetType()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Salary",
            Type = TransactionType.Income
        };

        context.Categories.Add(category);

        var budget = new Budget
        {
            Name = "Food budget May",
            Amount = 300m,
            Month = 5,
            Year = 2026,
            Type = TransactionType.Expense,
            CategoryId = null
        };

        context.Budgets.Add(budget);
        await context.SaveChangesAsync();

        var service = new BudgetService(context);

        var updateBudgetDto = new UpdateBudgetDto
        {
            Name = "Food budget May updated",
            Amount = 350m,
            Month = 5,
            Year = 2026,
            Type = TransactionType.Expense,
            CategoryId = 1
        };

        // Act
        var result = await service.UpdateAsync(budget.Id, updateBudgetDto);

        // Assert
        Assert.Equal(BudgetOperationResult.CategoryTypeMismatch, result);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCategoryTypeMismatch_WhenCategoryTypeDoesNotMatchBudgetType()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Salary",
            Type = TransactionType.Income
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = new BudgetService(context);

        var createBudgetDto = new CreateBudgetDto
        {
            Name = "Food budget May",
            Amount = 300m,
            Month = 5,
            Year = 2026,
            Type = TransactionType.Expense,
            CategoryId = 1
        };

        // Act
        var result = await service.CreateAsync(createBudgetDto);

        // Assert
        Assert.Equal(BudgetOperationResult.CategoryTypeMismatch, result.Result);
        Assert.Null(result.Budget);
    }
}