using FinanceTracker.Application.DTOs.Users;

namespace FinanceTracker.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> RegisterAsync(RegisterUserDto registerUserDto);

    Task<UserDto?> LoginAsync(LoginUserDto loginUserDto);

    Task<UserDto?> GetByIdAsync(int id);

    Task<UserDto?> GetByEmailAsync(string email);
}