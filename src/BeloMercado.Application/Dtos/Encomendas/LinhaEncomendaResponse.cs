namespace BeloMercado.Application.Dtos.Encomendas;

public class LinhaEncomendaResponse
{
    public Guid Id { get; set; }
    public Guid ProdutoId { get; set; }
    public string ProdutoEan13 { get; set; } = string.Empty;
    public string ProdutoDescricao { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoCustoUnitario { get; set; }
    public decimal DescontoPercentagem { get; set; }
    public decimal Subtotal { get; set; }
    public int QuantidadeRecebida { get; set; }
}
