using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Tests.Services;

public class DashboardServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldReturnDashboardSummary()
    {
        // Arrange
        using var context = CreateDbContext();

        var incomeCategory = new Category
        {
            Id = 1,
            Name = "Salary",
            Type = TransactionType.Income
        };

        var expenseCategory = new Category
        {
            Id = 2,
            Name = "Food",
            Type = TransactionType.Expense
        };

        context.Categories.AddRange(incomeCategory, expenseCategory);

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
                CategoryId = 2
            },
            new Transaction
            {
                Description = "Fuel",
                Amount = 50m,
                Date = new DateTime(2026, 5, 3),
                Type = TransactionType.Expense,
                CategoryId = 2
            }
        );

        await context.SaveChangesAsync();

        var service = new DashboardService(context);

        var filter = new TransactionFilterDto();

        // Act
        var result = await service.GetSummaryAsync(filter);

        // Assert
        Assert.Equal(2000m, result.TotalIncome);
        Assert.Equal(200m, result.TotalExpense);
        Assert.Equal(1800m, result.Balance);
        Assert.Equal(3, result.TransactionCount);
        Assert.Equal(3, result.LatestTransactions.Count);
        Assert.Equal("Fuel", result.LatestTransactions[0].Description);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldFilterTransactionsByDateRange()
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

        context.Transactions.AddRange(
            new Transaction
            {
                Description = "Old expense",
                Amount = 100m,
                Date = new DateTime(2026, 4, 1),
                Type = TransactionType.Expense,
                CategoryId = 1
            },
            new Transaction
            {
                Description = "Current expense",
                Amount = 50m,
                Date = new DateTime(2026, 5, 15),
                Type = TransactionType.Expense,
                CategoryId = 1
            }
        );

        await context.SaveChangesAsync();

        var service = new DashboardService(context);

        var filter = new TransactionFilterDto
        {
            FromDate = new DateTime(2026, 5, 1),
            ToDate = new DateTime(2026, 5, 31)
        };

        // Act
        var result = await service.GetSummaryAsync(filter);

        // Assert
        Assert.Equal(0m, result.TotalIncome);
        Assert.Equal(50m, result.TotalExpense);
        Assert.Equal(-50m, result.Balance);
        Assert.Equal(1, result.TransactionCount);
        Assert.Single(result.LatestTransactions);
        Assert.Equal("Current expense", result.LatestTransactions[0].Description);
    }
}