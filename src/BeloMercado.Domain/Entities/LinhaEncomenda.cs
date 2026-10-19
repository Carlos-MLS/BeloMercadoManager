namespace BeloMercado.Domain.Entities;

public class LinhaEncomenda
{
    public Guid Id { get; set; }
    public Guid EncomendaId { get; set; }
    public Guid ProdutoId { get; set; }

    public string ProdutoDescricao { get; set; } = string.Empty;
    public string ProdutoEan13 { get; set; } = string.Empty;

    public int Quantidade { get; set; }

    /// <summary>Preço de custo unitário no momento de criação da encomenda (snapshot).</summary>
    public decimal PrecoCustoUnitario { get; set; }
    public decimal DescontoPercentagem { get; set; }
    public decimal Subtotal { get; set; }

    /// <summary>Quantidade já rececionada (UC11). 0 enquanto a encomenda não é recebida.</summary>
    public int QuantidadeRecebida { get; set; }

    public Encomenda? Encomenda { get; set; }
    public Produto? Produto { get; set; }
}
