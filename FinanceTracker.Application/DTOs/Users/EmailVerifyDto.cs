using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Application.DTOs.Users;

public class EmailVerifyDto
{
    [Required]
    public string VerificationToken { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{6}$")]
    public string Code { get; set; } = string.Empty;
}

public class EmailResendDto
{
    [Required]
    public string VerificationToken { get; set; } = string.Empty;
}
