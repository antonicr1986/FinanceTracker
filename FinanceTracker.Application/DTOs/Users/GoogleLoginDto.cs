using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Application.DTOs.Users;

public class GoogleLoginDto
{
    /// <summary>
    /// ID token que el cliente obtiene de Google al elegir la cuenta.
    /// </summary>
    [Required]
    public string IdToken { get; set; } = string.Empty;

    /// <summary>
    /// Idioma del cliente ("es" o "en") para las categorias de partida si se
    /// crea la cuenta. Opcional, como en el registro.
    /// </summary>
    [MaxLength(10)]
    public string? Language { get; set; }
}
