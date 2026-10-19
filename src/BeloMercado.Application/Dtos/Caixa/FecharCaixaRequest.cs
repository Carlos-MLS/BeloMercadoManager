using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Caixa;

public class FecharCaixaRequest
{
    [Range(0, 99999.99)]
    public decimal ValorContado { get; set; }

    [MaxLength(512)]
    public string? Justificacao { get; set; }
}
