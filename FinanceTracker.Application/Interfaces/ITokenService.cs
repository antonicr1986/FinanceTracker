using FinanceTracker.Application.DTOs.Users;

namespace FinanceTracker.Application.Interfaces;

public interface ITokenService
{
    LoginResponseDto CreateToken(UserDto user);
}