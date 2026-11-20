using FinanceTracker.Application.DTOs.Mfa;
using FinanceTracker.Application.DTOs.Users;

namespace FinanceTracker.Application.Interfaces;

public interface ITokenService
{
    LoginResponseDto CreateToken(UserDto user);

    /// <summary>Token temporal entre la contraseña y el codigo del MFA.</summary>
    MfaChallengeDto CreateMfaChallenge(int userId);

    /// <summary>Id del usuario si el token de MFA es valido y no ha caducado; si no, null.</summary>
    int? ValidateMfaChallenge(string mfaToken);

    /// <summary>Token temporal entre la contraseña y el codigo enviado por email.</summary>
    EmailVerificationChallengeDto CreateEmailVerificationChallenge(int userId, string email);

    /// <summary>Id del usuario si el token de verificacion es valido y no ha caducado; si no, null.</summary>
    int? ValidateEmailVerificationChallenge(string verificationToken);
}
