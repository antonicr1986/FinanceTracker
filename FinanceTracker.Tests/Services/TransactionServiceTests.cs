using FinanceTracker.Application.Common;
using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Tests.Services;

public class TransactionServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldReturnCorrectTotals()
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

        context.Transactions.AddRange(
            new Transaction
            {
                Description = "Monthly salary",
                Amount = 2000m,
                Date = new DateTime(2026, 5, 1),
                Type = TransactionType.Income,
                CategoryId = 1
            },
            new Transaction
            {
                Description = "Groceries",
                Amount = 150m,
                Date = new DateTime(2026, 5, 2),
                Type = TransactionType.Expense,
                CategoryId = 1
            },
            new Transaction
            {
                Description = "Fuel",
                Amount = 50m,
                Date = new DateTime(2026, 5, 3),
                Type = TransactionType.Expense,
                CategoryId = 1
            }
        );

        await context.SaveChangesAsync();

        var service = new TransactionService(context);

        var filter = new TransactionFilterDto();

        // Act
        var result = await service.GetSummaryAsync(filter);

        // Assert
        Assert.Equal(2000m, result.TotalIncome);
        Assert.Equal(200m, result.TotalExpense);
        Assert.Equal(1800m, result.Balance);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterTransactionsByType()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "General",
            Type = TransactionType.Expense
        };

        context.Categories.Add(category);

        context.Transactions.AddRange(
            new Transaction
            {
                Description = "Salary",
                Amount = 2000m,
                Date = new DateTime(2026, 5, 1),
                Type = TransactionType.Income,
                CategoryId = 1
            },
            new Transaction
            {
                Description = "Groceries",
                Amount = 150m,
                Date = new DateTime(2026, 5, 2),
                Type = TransactionType.Expense,
                CategoryId = 1
            }
        );

        await context.SaveChangesAsync();

        var service = new TransactionService(context);

        var filter = new TransactionFilterDto
        {
            Type = TransactionType.Expense
        };

        // Act
        var result = await service.GetAllAsync(filter);

        // Assert
        Assert.Single(result);
        Assert.Equal("Groceries", result[0].Description);
        Assert.Equal(TransactionType.Expense, result[0].Type);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnNull_WhenCategoryDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();

        var service = new TransactionService(context);

        var createTransactionDto = new CreateTransactionDto
        {
            Description = "Invalid transaction",
            Amount = 100m,
            Date = new DateTime(2026, 5, 18),
            Type = TransactionType.Expense,
            CategoryId = 999
        };

        // Act
        var result = await service.CreateAsync(createTransactionDto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPaginatedResults()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "General",
            Type = TransactionType.Expense
        };

        context.Categories.Add(category);

        context.Transactions.AddRange(
            new Transaction
            {
                Description = "Old transaction",
                Amount = 10m,
                Date = new DateTime(2026, 5, 1),
                Type = TransactionType.Expense,
                CategoryId = 1
            },
            new Transaction
            {
                Description = "New transaction",
                Amount = 20m,
                Date = new DateTime(2026, 5, 20),
                Type = TransactionType.Expense,
                CategoryId = 1
            }
        );

        await context.SaveChangesAsync();

        var service = new TransactionService(context);

        var filter = new TransactionFilterDto
        {
            PageNumber = 1,
            PageSize = 1
        };

        // Act
        var result = await service.GetAllAsync(filter);

        // Assert
        Assert.Single(result);
        Assert.Equal("New transaction", result[0].Description);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateTransaction_WhenCategoryExists()
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

        var service = new TransactionService(context);

        var createTransactionDto = new CreateTransactionDto
        {
            Description = "Groceries",
            Amount = 150m,
            Date = new DateTime(2026, 5, 18),
            Type = TransactionType.Expense,
            CategoryId = 1
        };

        // Act
        var result = await service.CreateAsync(createTransactionDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Groceries", result.Description);
        Assert.Equal(150m, result.Amount);
        Assert.Equal(TransactionType.Expense, result.Type);
        Assert.Equal(1, result.CategoryId);
        Assert.Equal("Food", result.CategoryName);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTransaction_WhenTransactionExists()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Transport",
            Type = TransactionType.Expense
        };

        context.Categories.Add(category);

        var transaction = new Transaction
        {
            Description = "Fuel",
            Amount = 50m,
            Date = new DateTime(2026, 5, 3),
            Type = TransactionType.Expense,
            CategoryId = 1
        };

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        var service = new TransactionService(context);

        // Act
        var result = await service.GetByIdAsync(transaction.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Fuel", result.Description);
        Assert.Equal(50m, result.Amount);
        Assert.Equal(TransactionType.Expense, result.Type);
        Assert.Equal("Transport", result.CategoryName);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenTransactionDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();

        var service = new TransactionService(context);

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnCategoryTypeMismatch_WhenCategoryTypeDoesNotMatchTransactionType()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Salary",
            Type = TransactionType.Income
        };

        var transaction = new Transaction
        {
            Description = "Groceries",
            Amount = 100m,
            Date = new DateTime(2026, 5, 23),
            Type = TransactionType.Expense,
            CategoryId = 1
        };

        context.Categories.Add(category);
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        var service = new TransactionService(context);

        var updateTransactionDto = new UpdateTransactionDto
        {
            Description = "Updated groceries",
            Amount = 120m,
            Date = new DateTime(2026, 5, 23),
            Type = TransactionType.Expense,
            CategoryId = 1
        };

        // Act
        var result = await service.UpdateAsync(transaction.Id, updateTransactionDto);

        // Assert
        Assert.Equal(UpdateTransactionResult.CategoryTypeMismatch, result);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnNull_WhenCategoryTypeDoesNotMatchTransactionType()
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

        var service = new TransactionService(context);

        var createTransactionDto = new CreateTransactionDto
        {
            Description = "Groceries",
            Amount = 100m,
            Date = new DateTime(2026, 5, 23),
            Type = TransactionType.Expense,
            CategoryId = 1
        };

        // Act
        var result = await service.CreateAsync(createTransactionDto);

        // Assert
        Assert.Null(result);
    }
}