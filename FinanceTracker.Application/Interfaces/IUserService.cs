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

    /// <summary>
    /// Cambia la contraseña pidiendo la actual. Si sale bien, cierra todas las
    /// sesiones abiertas: quien cambia la contraseña porque sospecha que se la
    /// han robado espera que el intruso quede fuera.
    /// </summary>
    Task<ChangePasswordResult> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
}
