using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Encomendas;

public class RececionarEncomendaRequest
{
    [Required, MinLength(1)]
    public List<LinhaRececionadaRequest> Linhas { get; set; } = new();
}
