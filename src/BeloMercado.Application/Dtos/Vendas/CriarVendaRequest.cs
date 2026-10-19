using System.ComponentModel.DataAnnotations;
using BeloMercado.Domain.Enums;

namespace BeloMercado.Application.Dtos.Vendas;

public class CriarVendaRequest
{
    [Required, MinLength(1)]
    public List<LinhaVendaRequest> Linhas { get; set; } = new();

    [Required]
    public MetodoPagamento MetodoPagamento { get; set; }

    /// <summary>Obrigatório quando MetodoPagamento = Numerario.</summary>
    [Range(0, 99999.99)]
    public decimal? ValorRecebido { get; set; }

    public ClienteRequest? Cliente { get; set; }
}
