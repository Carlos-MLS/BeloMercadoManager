using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Me;

public class ChangePasswordRequest
{
    [Required, MaxLength(128)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}
