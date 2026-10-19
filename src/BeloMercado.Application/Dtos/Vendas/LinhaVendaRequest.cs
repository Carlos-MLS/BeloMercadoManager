using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Vendas;

public class LinhaVendaRequest
{
    [Required]
    public Guid ProdutoId { get; set; }

    [Range(1, 9999)]
    public int Quantidade { get; set; }
}
