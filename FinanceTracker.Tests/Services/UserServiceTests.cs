using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Interfaces;
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

    // Servicio para los tests que no usan Google.
    private static UserService CreateService(AppDbContext context) =>
        new(context, new FakeGoogleTokenValidator(null));

    // Sustituye a Google en los tests: devuelve la identidad configurada
    // para cualquier token, o null para simular un token no valido.
    private class FakeGoogleTokenValidator : IGoogleTokenValidator
    {
        private readonly GoogleIdentity? _identity;

        public FakeGoogleTokenValidator(GoogleIdentity? identity) => _identity = identity;

        public Task<GoogleIdentity?> ValidateAsync(string idToken) => Task.FromResult(_identity);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenEmailDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();

        var service = CreateService(context);

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

        var service = CreateService(context);

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

        var service = CreateService(context);

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

        var service = CreateService(context);

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

        var service = CreateService(context);

        // Act
        var result = await service.GetByEmailAsync("antonio@test.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Antonio", result.Name);
        Assert.Equal("antonio@test.com", result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();

        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNull_WhenEmailDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();

        var service = CreateService(context);

        // Act
        var result = await service.GetByEmailAsync("missing@test.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnUser_WhenCredentialsAreValid()
    {
        // Arrange
        using var context = CreateDbContext();

        var user = new User
        {
            Name = "Antonio",
            Email = "antonio@test.com"
        };

        var passwordHasher = new PasswordHasher<User>();
        user.PasswordHash = passwordHasher.HashPassword(user, "123456");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var loginUserDto = new LoginUserDto
        {
            Email = "antonio@test.com",
            Password = "123456"
        };

        // Act
        var result = await service.LoginAsync(loginUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Antonio", result.Name);
        Assert.Equal("antonio@test.com", result.Email);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_WhenPasswordIsInvalid()
    {
        // Arrange
        using var context = CreateDbContext();

        var user = new User
        {
            Name = "Antonio",
            Email = "antonio@test.com"
        };

        var passwordHasher = new PasswordHasher<User>();
        user.PasswordHash = passwordHasher.HashPassword(user, "123456");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var loginUserDto = new LoginUserDto
        {
            Email = "antonio@test.com",
            Password = "wrong-password"
        };

        // Act
        var result = await service.LoginAsync(loginUserDto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_WhenEmailDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();

        var service = CreateService(context);

        var loginUserDto = new LoginUserDto
        {
            Email = "missing@test.com",
            Password = "123456"
        };

        // Act
        var result = await service.LoginAsync(loginUserDto);

        // Assert
        Assert.Null(result);
    }

    private static async Task<List<string>> RegisterAndGetCategoryNames(string? language)
    {
        using var context = CreateDbContext();
        var service = CreateService(context);

        var user = await service.RegisterAsync(new RegisterUserDto
        {
            Name = "Antonio",
            Email = $"antonio-{Guid.NewGuid()}@test.com",
            Password = "123456",
            Language = language
        });

        return await context.Categories
            .Where(category => category.UserId == user!.Id)
            .Select(category => category.Name)
            .ToListAsync();
    }

    [Fact]
    public async Task RegisterAsync_ShouldSeedStarterCategoriesInEnglish_WhenLanguageIsEnglish()
    {
        var names = await RegisterAndGetCategoryNames("en");

        Assert.Equal(8, names.Count);
        Assert.Contains("Groceries", names);
        Assert.Contains("Salary", names);
        Assert.DoesNotContain("Supermercado", names);
    }

    [Fact]
    public async Task RegisterAsync_ShouldSeedStarterCategoriesInSpanish_WhenLanguageIsSpanish()
    {
        var names = await RegisterAndGetCategoryNames("es");

        Assert.Equal(8, names.Count);
        Assert.Contains("Supermercado", names);
        Assert.Contains("Nómina", names);
    }

    [Fact]
    public async Task RegisterAsync_ShouldSeedStarterCategoriesInSpanish_WhenNoLanguageIsSent()
    {
        // Los clientes que todavia no envian el idioma siguen igual que antes.
        var names = await RegisterAndGetCategoryNames(null);

        Assert.Contains("Supermercado", names);
    }

    [Theory]
    [InlineData("EN")]
    [InlineData("en-GB")]
    [InlineData(" en-US ")]
    public void DefaultCategories_ShouldTreatAnyEnglishVariantAsEnglish(string language)
    {
        Assert.True(DefaultCategories.IsEnglish(language));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("")]
    [InlineData(null)]
    public void DefaultCategories_ShouldFallBackToSpanish_ForAnyOtherLanguage(string? language)
    {
        Assert.False(DefaultCategories.IsEnglish(language));
        Assert.Contains(DefaultCategories.ForUser(1, language), category => category.Name == "Supermercado");
    }

    [Fact]
    public void DefaultCategories_ShouldHaveTheSameTypesInBothLanguages()
    {
        var spanish = DefaultCategories.ForUser(1, "es").Select(category => category.Type);
        var english = DefaultCategories.ForUser(1, "en").Select(category => category.Type);

        Assert.Equal(spanish, english);
    }

    [Fact]
    public async Task GoogleLoginAsync_ShouldCreateUser_WhenNoAccountExists()
    {
        // Arrange
        using var context = CreateDbContext();

        var google = new FakeGoogleTokenValidator(
            new GoogleIdentity("google-sub-123", "maria@gmail.com", EmailVerified: true, "María"));

        var service = new UserService(context, google);

        // Act
        var result = await service.GoogleLoginAsync("id-token", language: "es");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("María", result.Name);
        Assert.Equal("maria@gmail.com", result.Email);

        var savedUser = await context.Users.SingleAsync();

        Assert.Equal("google-sub-123", savedUser.GoogleSubject);
    }

    [Fact]
    public async Task GoogleLoginAsync_ShouldSeedDefaultCategories_InRequestedLanguage()
    {
        // Arrange
        using var context = CreateDbContext();

        var google = new FakeGoogleTokenValidator(
            new GoogleIdentity("google-sub-123", "maria@gmail.com", EmailVerified: true, "María"));

        var service = new UserService(context, google);

        // Act
        var result = await service.GoogleLoginAsync("id-token", language: "en");

        // Assert
        Assert.NotNull(result);

        var names = await context.Categories
            .Where(category => category.UserId == result.Id)
            .Select(category => category.Name)
            .ToListAsync();

        Assert.Contains("Groceries", names);
        Assert.DoesNotContain("Supermercado", names);
    }

    [Fact]
    public async Task GoogleLoginAsync_ShouldLinkExistingAccount_WhenEmailMatches()
    {
        // Arrange
        using var context = CreateDbContext();

        var google = new FakeGoogleTokenValidator(
            new GoogleIdentity("google-sub-123", "antonio@test.com", EmailVerified: true, "Antonio G."));

        var service = new UserService(context, google);

        var existing = await service.RegisterAsync(new RegisterUserDto
        {
            Name = "Antonio",
            Email = "antonio@test.com",
            Password = "123456"
        });

        // Act
        var result = await service.GoogleLoginAsync("id-token", language: "es");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existing!.Id, result.Id);

        var savedUser = await context.Users.SingleAsync();

        Assert.Equal("google-sub-123", savedUser.GoogleSubject);
    }

    [Fact]
    public async Task LoginAsync_ShouldStillWorkWithPassword_AfterGoogleLinking()
    {
        // Arrange
        using var context = CreateDbContext();

        var google = new FakeGoogleTokenValidator(
            new GoogleIdentity("google-sub-123", "antonio@test.com", EmailVerified: true, "Antonio G."));

        var service = new UserService(context, google);

        await service.RegisterAsync(new RegisterUserDto
        {
            Name = "Antonio",
            Email = "antonio@test.com",
            Password = "123456"
        });

        await service.GoogleLoginAsync("id-token", language: "es");

        // Act
        var result = await service.LoginAsync(new LoginUserDto
        {
            Email = "antonio@test.com",
            Password = "123456"
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("antonio@test.com", result.Email);
    }

    [Fact]
    public async Task GoogleLoginAsync_ShouldFindUserBySubject_WhenGoogleEmailChanged()
    {
        // Arrange
        using var context = CreateDbContext();

        var firstLogin = new UserService(context, new FakeGoogleTokenValidator(
            new GoogleIdentity("google-sub-123", "maria@gmail.com", EmailVerified: true, "María")));

        var created = await firstLogin.GoogleLoginAsync("id-token", language: "es");

        // La misma cuenta de Google (mismo sub) con otro email.
        var service = new UserService(context, new FakeGoogleTokenValidator(
            new GoogleIdentity("google-sub-123", "maria.nuevo@gmail.com", EmailVerified: true, "María")));

        // Act
        var result = await service.GoogleLoginAsync("id-token", language: "es");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created!.Id, result.Id);
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Fact]
    public async Task GoogleLoginAsync_ShouldReturnNull_WhenEmailNotVerified()
    {
        // Arrange
        using var context = CreateDbContext();

        // Google no garantiza que este email sea de quien inicia sesion:
        // vincularlo daria acceso a la cuenta de otra persona.
        var google = new FakeGoogleTokenValidator(
            new GoogleIdentity("google-sub-999", "antonio@test.com", EmailVerified: false, "Otra persona"));

        var service = new UserService(context, google);

        await service.RegisterAsync(new RegisterUserDto
        {
            Name = "Antonio",
            Email = "antonio@test.com",
            Password = "123456"
        });

        // Act
        var result = await service.GoogleLoginAsync("id-token", language: "es");

        // Assert
        Assert.Null(result);

        var savedUser = await context.Users.SingleAsync();

        Assert.Null(savedUser.GoogleSubject);
    }

    [Fact]
    public async Task GoogleLoginAsync_ShouldReturnNull_WhenTokenIsInvalid()
    {
        // Arrange
        using var context = CreateDbContext();

        var service = new UserService(context, new FakeGoogleTokenValidator(null));

        // Act
        var result = await service.GoogleLoginAsync("token-no-valido", language: "es");

        // Assert
        Assert.Null(result);
        Assert.Equal(0, await context.Users.CountAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456")]
    public async Task LoginAsync_ShouldReturnNull_ForGoogleOnlyAccount(string password)
    {
        // Arrange
        using var context = CreateDbContext();

        var service = new UserService(context, new FakeGoogleTokenValidator(
            new GoogleIdentity("google-sub-123", "maria@gmail.com", EmailVerified: true, "María")));

        // La cuenta creada con Google no tiene contraseña.
        await service.GoogleLoginAsync("id-token", language: "es");

        // Act
        var result = await service.LoginAsync(new LoginUserDto
        {
            Email = "maria@gmail.com",
            Password = password
        });

        // Assert
        Assert.Null(result);
    }
}
