using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Encomendas;

public class LinhaRececionadaRequest
{
    [Required]
    public Guid LinhaId { get; set; }

    [Range(0, 99999)]
    public int QuantidadeRecebida { get; set; }
}
