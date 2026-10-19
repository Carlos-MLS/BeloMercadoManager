namespace BeloMercado.Domain.Entities;

/// <summary>
/// Histórico: regista que uma promoção foi aplicada a uma linha de venda.
/// Usado em análise/relatórios; não afeta o cálculo do total (esse já está
/// snapshot-ado em LinhaVenda.Subtotal/PrecoUnitario).
/// </summary>
public class LinhaVendaPromocao
{
    public Guid LinhaVendaId { get; set; }
    public Guid PromocaoId { get; set; }
    public decimal ValorDescontoAplicado { get; set; }

    public LinhaVenda? LinhaVenda { get; set; }
    public Promocao? Promocao { get; set; }
}
