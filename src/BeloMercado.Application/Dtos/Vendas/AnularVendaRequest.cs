using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Vendas;

public class AnularVendaRequest
{
    [Required, MaxLength(512)]
    public string Motivo { get; set; } = string.Empty;
}
