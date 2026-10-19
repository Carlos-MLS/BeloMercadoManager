using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Auth;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
