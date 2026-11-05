using FinanceTracker.Api.Controllers;
using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Tests.Api;

public class UsersControllerTests
{
    private class FakeCurrentUser : ICurrentUserService
    {
        public FakeCurrentUser(int? userId) => UserId = userId;

        public int? UserId { get; }
    }

    private class FakeUserService : IUserService
    {
        private readonly UserDto? _googleUser;

        public FakeUserService(UserDto? googleUser = null) => _googleUser = googleUser;

        public Task<UserDto?> GetByIdAsync(int id) =>
            Task.FromResult<UserDto?>(new UserDto { Id = id, Name = "User " + id, Email = $"user{id}@test.com" });

        public Task<UserDto?> RegisterAsync(RegisterUserDto registerUserDto) => throw new NotImplementedException();

        public Task<UserDto?> LoginAsync(LoginUserDto loginUserDto) => throw new NotImplementedException();

        public Task<UserDto?> GetByEmailAsync(string email) => throw new NotImplementedException();

        public Task<UserDto?> GoogleLoginAsync(string idToken, string? language) => Task.FromResult(_googleUser);
    }

    private class FakeTokenService : ITokenService
    {
        public LoginResponseDto CreateToken(UserDto user) => new() { Token = "jwt-token", User = user };
    }

    private static UsersController ControllerFor(int? currentUserId) =>
        new(new FakeUserService(), new FakeTokenService(), new FakeCurrentUser(currentUserId));

    private static UsersController ControllerWithGoogleUser(UserDto? googleUser) =>
        new(new FakeUserService(googleUser), new FakeTokenService(), new FakeCurrentUser(null));

    [Fact]
    public async Task GetById_ShouldReturnOwnUser()
    {
        var result = await ControllerFor(currentUserId: 7).GetById(7);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(7, Assert.IsType<UserDto>(ok.Value).Id);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(1)]
    public async Task GetById_ShouldReturnNotFound_ForAnotherUser(int otherId)
    {
        // Antes devolvia el nombre y el email de cualquier usuario por su id.
        var result = await ControllerFor(currentUserId: 7).GetById(otherId);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WithoutSession()
    {
        var result = await ControllerFor(currentUserId: null).GetById(7);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public void GetById_ShouldRequireAuthentication()
    {
        var method = typeof(UsersController).GetMethod(nameof(UsersController.GetById))!;

        Assert.NotEmpty(method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
    }

    [Fact]
    public async Task GoogleLogin_ShouldReturnToken_WhenGoogleLoginSucceeds()
    {
        var user = new UserDto { Id = 7, Name = "María", Email = "maria@gmail.com" };

        var result = await ControllerWithGoogleUser(user)
            .GoogleLogin(new GoogleLoginDto { IdToken = "id-token", Language = "es" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<LoginResponseDto>(ok.Value);
        Assert.Equal("jwt-token", response.Token);
        Assert.Equal(7, response.User.Id);
    }

    [Fact]
    public async Task GoogleLogin_ShouldReturnUnauthorized_WhenGoogleLoginFails()
    {
        var result = await ControllerWithGoogleUser(null)
            .GoogleLogin(new GoogleLoginDto { IdToken = "token-no-valido" });

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(401, problem.StatusCode);
        Assert.Equal("invalid_google_token", Assert.IsType<ProblemDetails>(problem.Value).Extensions["code"]);
    }
}
