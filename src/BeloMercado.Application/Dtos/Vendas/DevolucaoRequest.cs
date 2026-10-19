using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Vendas;

public class DevolucaoRequest
{
    [Required, MinLength(1)]
    public List<LinhaDevolucaoRequest> Linhas { get; set; } = new();

    [MaxLength(512)]
    public string? Motivo { get; set; }
}
