using FinanceTracker.Application.Common;
using FinanceTracker.Application.DTOs.Categories;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Tests.Services;

public class CategoryServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private class TestCurrentUserService : ICurrentUserService
    {
        public int? UserId { get; }

        public TestCurrentUserService(int? userId)
        {
            UserId = userId;
        }
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCategories()
    {
        // Arrange
        using var context = CreateDbContext();

        context.Categories.AddRange(
            new Category
            {
                Id = 1,
                Name = "Food",
                Type = TransactionType.Expense,
                UserId = 1
            },
            new Category
            {
                Id = 2,
                Name = "Salary",
                Type = TransactionType.Income,
                UserId = 1
            }
        );

        await context.SaveChangesAsync();

        var service = new CategoryService(context, new TestCurrentUserService(1));

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, category => category.Name == "Food");
        Assert.Contains(result, category => category.Name == "Salary");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCategory_WhenCategoryExists()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Type = TransactionType.Expense,
            UserId = 1
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = new CategoryService(context, new TestCurrentUserService(1));

        // Act
        var result = await service.GetByIdAsync(category.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Food", result.Name);
        Assert.Equal(TransactionType.Expense, result.Type);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenCategoryDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();

        var service = new CategoryService(context, new TestCurrentUserService(1));

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateCategory_AndAssignItToCurrentUser()
    {
        // Arrange
        using var context = CreateDbContext();

        var service = new CategoryService(context, new TestCurrentUserService(1));

        var createCategoryDto = new CreateCategoryDto
        {
            Name = "Transport",
            Type = TransactionType.Expense
        };

        // Act
        var result = await service.CreateAsync(createCategoryDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Transport", result.Name);
        Assert.Equal(TransactionType.Expense, result.Type);

        // La categoria queda asociada al usuario actual, no huerfana
        var stored = await context.Categories.FindAsync(result.Id);

        Assert.NotNull(stored);
        Assert.Equal(1, stored.UserId);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCategory_WhenCategoryExists()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Type = TransactionType.Expense,
            UserId = 1
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = new CategoryService(context, new TestCurrentUserService(1));

        var updateCategoryDto = new UpdateCategoryDto
        {
            Name = "Groceries",
            Type = TransactionType.Expense
        };

        // Act
        var result = await service.UpdateAsync(category.Id, updateCategoryDto);

        // Assert
        Assert.True(result);

        var updatedCategory = await context.Categories.FindAsync(category.Id);

        Assert.NotNull(updatedCategory);
        Assert.Equal("Groceries", updatedCategory.Name);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenCategoryDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();

        var service = new CategoryService(context, new TestCurrentUserService(1));

        var updateCategoryDto = new UpdateCategoryDto
        {
            Name = "Groceries",
            Type = TransactionType.Expense
        };

        // Act
        var result = await service.UpdateAsync(999, updateCategoryDto);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnSuccess_WhenCategoryHasNoTransactions()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Type = TransactionType.Expense,
            UserId = 1
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = new CategoryService(context, new TestCurrentUserService(1));

        // Act
        var result = await service.DeleteAsync(category.Id);

        // Assert
        Assert.Equal(DeleteCategoryResult.Success, result);

        var deletedCategory = await context.Categories.FindAsync(category.Id);
        Assert.Null(deletedCategory);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnCategoryNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();

        var service = new CategoryService(context, new TestCurrentUserService(1));

        // Act
        var result = await service.DeleteAsync(999);

        // Assert
        Assert.Equal(DeleteCategoryResult.CategoryNotFound, result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnCategoryHasTransactions_WhenCategoryIsInUse()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Type = TransactionType.Expense,
            UserId = 1
        };

        context.Categories.Add(category);

        context.Transactions.Add(new Transaction
        {
            Description = "Groceries",
            Amount = 100m,
            Date = new DateTime(2026, 5, 10),
            Type = TransactionType.Expense,
            CategoryId = 1,
            UserId = 1
        });

        await context.SaveChangesAsync();

        var service = new CategoryService(context, new TestCurrentUserService(1));

        // Act
        var result = await service.DeleteAsync(category.Id);

        // Assert
        Assert.Equal(DeleteCategoryResult.CategoryHasTransactions, result);

        // La categoria no se ha borrado
        var stillThere = await context.Categories.FindAsync(category.Id);
        Assert.NotNull(stillThere);
    }

    [Fact]
    public async Task GetAllAsync_ShouldNotReturnCategories_OfAnotherUser()
    {
        // Arrange
        using var context = CreateDbContext();

        context.Categories.Add(new Category
        {
            Id = 1,
            Name = "Food",
            Type = TransactionType.Expense,
            UserId = 1
        });

        await context.SaveChangesAsync();

        // El servicio actua como el usuario 2
        var service = new CategoryService(context, new TestCurrentUserService(2));

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenCategoryBelongsToAnotherUser()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Type = TransactionType.Expense,
            UserId = 1
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = new CategoryService(context, new TestCurrentUserService(2));

        // Act
        var result = await service.GetByIdAsync(category.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotUpdateCategory_OfAnotherUser()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Type = TransactionType.Expense,
            UserId = 1
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = new CategoryService(context, new TestCurrentUserService(2));

        var updateCategoryDto = new UpdateCategoryDto
        {
            Name = "Hijacked",
            Type = TransactionType.Income
        };

        // Act
        var result = await service.UpdateAsync(category.Id, updateCategoryDto);

        // Assert
        Assert.False(result);

        // La categoria del usuario 1 sigue intacta
        var untouched = await context.Categories.FindAsync(category.Id);

        Assert.NotNull(untouched);
        Assert.Equal("Food", untouched.Name);
        Assert.Equal(TransactionType.Expense, untouched.Type);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotDeleteCategory_OfAnotherUser()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Type = TransactionType.Expense,
            UserId = 1
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = new CategoryService(context, new TestCurrentUserService(2));

        // Act
        var result = await service.DeleteAsync(category.Id);

        // Assert
        Assert.Equal(DeleteCategoryResult.CategoryNotFound, result);

        // La categoria del usuario 1 sigue ahi
        var stillThere = await context.Categories.FindAsync(category.Id);
        Assert.NotNull(stillThere);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnSuccess_WhenOnlyAnotherUsersTransactionsUseTheCategory()
    {
        // Arrange
        using var context = CreateDbContext();

        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Type = TransactionType.Expense,
            UserId = 1
        };

        context.Categories.Add(category);

        // Una transaccion de OTRO usuario que apunta a la misma categoria
        context.Transactions.Add(new Transaction
        {
            Description = "Groceries",
            Amount = 100m,
            Date = new DateTime(2026, 5, 10),
            Type = TransactionType.Expense,
            CategoryId = 1,
            UserId = 2
        });

        await context.SaveChangesAsync();

        var service = new CategoryService(context, new TestCurrentUserService(1));

        // Act
        var result = await service.DeleteAsync(category.Id);

        // Assert
        // La transaccion ajena no cuenta como "en uso" para el usuario 1
        Assert.Equal(DeleteCategoryResult.Success, result);
    }
}
