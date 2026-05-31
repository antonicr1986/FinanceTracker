using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Tests.Services;

public class UserServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenEmailDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();

        var service = new UserService(context);

        var registerUserDto = new RegisterUserDto
        {
            Name = "Antonio",
            Email = "antonio@test.com",
            Password = "123456"
        };

        // Act
        var result = await service.RegisterAsync(registerUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Antonio", result.Name);
        Assert.Equal("antonio@test.com", result.Email);

        var savedUser = await context.Users.SingleAsync();

        Assert.NotEqual("123456", savedUser.PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_ShouldStoreHashedPassword()
    {
        // Arrange
        using var context = CreateDbContext();

        var service = new UserService(context);

        var registerUserDto = new RegisterUserDto
        {
            Name = "Antonio",
            Email = "antonio@test.com",
            Password = "123456"
        };

        // Act
        await service.RegisterAsync(registerUserDto);

        // Assert
        var savedUser = await context.Users.SingleAsync();

        var passwordHasher = new PasswordHasher<User>();

        var verificationResult = passwordHasher.VerifyHashedPassword(
            savedUser,
            savedUser.PasswordHash,
            "123456");

        Assert.NotEqual(PasswordVerificationResult.Failed, verificationResult);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnNull_WhenEmailAlreadyExists()
    {
        // Arrange
        using var context = CreateDbContext();

        context.Users.Add(new User
        {
            Name = "Existing user",
            Email = "antonio@test.com",
            PasswordHash = "hashed-password"
        });

        await context.SaveChangesAsync();

        var service = new UserService(context);

        var registerUserDto = new RegisterUserDto
        {
            Name = "Duplicated user",
            Email = "antonio@test.com",
            Password = "123456"
        };

        // Act
        var result = await service.RegisterAsync(registerUserDto);

        // Assert
        Assert.Null(result);
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        using var context = CreateDbContext();

        var user = new User
        {
            Name = "Antonio",
            Email = "antonio@test.com",
            PasswordHash = "hashed-password"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new UserService(context);

        // Act
        var result = await service.GetByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Antonio", result.Name);
        Assert.Equal("antonio@test.com", result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenEmailExists()
    {
        // Arrange
        using var context = CreateDbContext();

        context.Users.Add(new User
        {
            Name = "Antonio",
            Email = "antonio@test.com",
            PasswordHash = "hashed-password"
        });

        await context.SaveChangesAsync();

        var service = new UserService(context);

        // Act
        var result = await service.GetByEmailAsync("antonio@test.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Antonio", result.Name);
        Assert.Equal("antonio@test.com", result.Email);
    }
}