namespace BeloMercado.Domain.Entities;

/// <summary>
/// Top-N de produtos vendidos numa loja num dado dia. Materializado pelo
/// ConsolidationService (UC12). N rows por (LojaId, Data, ProdutoId).
/// </summary>
public class RankingProdutoDiario
{
    public Guid LojaId { get; set; }
    public DateOnly Data { get; set; }
    public Guid ProdutoId { get; set; }

    public int Posicao { get; set; }
    public int QuantidadeVendida { get; set; }
    public decimal Receita { get; set; }

    public Loja? Loja { get; set; }
    public Produto? Produto { get; set; }
}
