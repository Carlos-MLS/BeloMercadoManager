namespace BeloMercado.Application.Dtos.Dashboards;

public class RankingProdutoResponse
{
    public int Posicao { get; set; }
    public Guid ProdutoId { get; set; }
    public string ProdutoEan13 { get; set; } = string.Empty;
    public string ProdutoDescricao { get; set; } = string.Empty;
    public int QuantidadeVendida { get; set; }
    public decimal Receita { get; set; }
}
