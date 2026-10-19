using System.ComponentModel.DataAnnotations;
using BeloMercado.Domain.Enums;

namespace BeloMercado.Application.Dtos.Utilizadores;

public class UpdateUtilizadorRequest
{
    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    public Perfil Perfil { get; set; }

    public Guid? LojaId { get; set; }

    public bool Ativo { get; set; } = true;
}
