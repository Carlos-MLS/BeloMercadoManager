using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Me;

/// <summary>
/// UC17 self-update. Only fields the user is allowed to change appear here.
/// Email and Perfil are intentionally NOT exposed (RN01: cannot edit own
/// email/perfil — those are managed by Admin via UC16).
/// </summary>
public class UpdateMeRequest
{
    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;
}
