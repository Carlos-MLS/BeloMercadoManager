namespace BeloMercado.Domain.Entities;

public class ProdutoFornecedor
{
    public Guid FornecedorId { get; set; }
    public Guid ProdutoId { get; set; }
    public decimal PrecoCusto { get; set; }
    public decimal DescontoPercentagem { get; set; }
    public int PrazoEntregaDias { get; set; }

    public Fornecedor? Fornecedor { get; set; }
    public Produto? Produto { get; set; }
}
