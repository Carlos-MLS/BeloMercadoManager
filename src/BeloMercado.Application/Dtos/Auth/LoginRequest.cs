using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Auth;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}
