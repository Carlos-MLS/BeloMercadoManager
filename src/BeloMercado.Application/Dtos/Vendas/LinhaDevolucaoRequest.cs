using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Vendas;

public class LinhaDevolucaoRequest
{
    [Required]
    public Guid LinhaVendaId { get; set; }

    [Range(1, 9999)]
    public int Quantidade { get; set; }
}
