using System.ComponentModel.DataAnnotations;
using BeloMercado.Domain.Enums;

namespace BeloMercado.Application.Dtos.Utilizadores;

public class CreateUtilizadorRequest
{
    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public Perfil Perfil { get; set; }

    public Guid? LojaId { get; set; }
}
