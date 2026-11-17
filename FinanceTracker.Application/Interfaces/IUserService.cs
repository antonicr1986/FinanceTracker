using FinanceTracker.Application.Common;
using FinanceTracker.Application.DTOs.Users;

namespace FinanceTracker.Application.Interfaces;

public interface IUserService
{
    Task<RegisterUserResult> RegisterAsync(RegisterUserDto registerUserDto);

    Task<UserDto?> LoginAsync(LoginUserDto loginUserDto);

    Task<UserDto?> GetByIdAsync(int id);

    Task<UserDto?> GetByEmailAsync(string email);

    Task<UserDto?> GoogleLoginAsync(string idToken, string? language);
}
