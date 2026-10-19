using System.ComponentModel.DataAnnotations;
using BeloMercado.Domain.Enums;

namespace BeloMercado.Application.Dtos.Promocoes;

public class UpdatePromocaoRequest
{
    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public Guid? LojaId { get; set; }

    [Required]
    public TipoPromocao Tipo { get; set; }

    [Range(0.01, 99999.99)]
    public decimal Valor { get; set; }

    [Required]
    public DateOnly DataInicio { get; set; }

    [Required]
    public DateOnly DataFim { get; set; }

    public bool Ativo { get; set; } = true;
}
