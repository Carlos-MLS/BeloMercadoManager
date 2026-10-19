using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Fornecedores;

public class ProdutoFornecedorRequest
{
    [Required]
    public Guid ProdutoId { get; set; }

    [Range(0, 99999.99)]
    public decimal PrecoCusto { get; set; }

    [Range(0, 100)]
    public decimal DescontoPercentagem { get; set; }

    [Range(0, 365)]
    public int PrazoEntregaDias { get; set; }
}
