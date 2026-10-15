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
        public Task<UserDto?> GetByIdAsync(int id) =>
            Task.FromResult<UserDto?>(new UserDto { Id = id, Name = "User " + id, Email = $"user{id}@test.com" });

        public Task<UserDto?> RegisterAsync(RegisterUserDto registerUserDto) => throw new NotImplementedException();

        public Task<UserDto?> LoginAsync(LoginUserDto loginUserDto) => throw new NotImplementedException();

        public Task<UserDto?> GetByEmailAsync(string email) => throw new NotImplementedException();
    }

    private class FakeTokenService : ITokenService
    {
        public LoginResponseDto CreateToken(UserDto user) => throw new NotImplementedException();
    }

    private static UsersController ControllerFor(int? currentUserId) =>
        new(new FakeUserService(), new FakeTokenService(), new FakeCurrentUser(currentUserId));

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
}
