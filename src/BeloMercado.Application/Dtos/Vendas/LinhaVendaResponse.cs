namespace BeloMercado.Application.Dtos.Vendas;

public class LinhaVendaResponse
{
    public Guid Id { get; set; }
    public Guid ProdutoId { get; set; }
    public string ProdutoEan13 { get; set; } = string.Empty;
    public string ProdutoDescricao { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public int TaxaIvaPercentagem { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ValorIva { get; set; }
    public int QuantidadeDevolvida { get; set; }
}
