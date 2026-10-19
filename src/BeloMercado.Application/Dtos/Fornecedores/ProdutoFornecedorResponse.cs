namespace BeloMercado.Application.Dtos.Fornecedores;

public class ProdutoFornecedorResponse
{
    public Guid ProdutoId { get; set; }
    public string ProdutoEan13 { get; set; } = string.Empty;
    public string ProdutoDescricao { get; set; } = string.Empty;
    public decimal PrecoCusto { get; set; }
    public decimal DescontoPercentagem { get; set; }
    public int PrazoEntregaDias { get; set; }
}
