using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Application.DTOs.Users;

public class RegisterUserDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Idioma del cliente al registrarse ("es" o "en"), para sembrar las
    /// categorias de partida en ese idioma. Opcional: sin el, o con cualquier
    /// otro valor, se siembran en español, como antes de existir este campo.
    /// </summary>
    [MaxLength(10)]
    public string? Language { get; set; }
}
