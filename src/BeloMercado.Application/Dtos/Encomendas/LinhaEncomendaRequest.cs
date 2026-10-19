using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Encomendas;

public class LinhaEncomendaRequest
{
    [Required]
    public Guid ProdutoId { get; set; }

    [Range(1, 99999)]
    public int Quantidade { get; set; }
}
