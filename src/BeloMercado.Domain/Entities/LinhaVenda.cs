namespace BeloMercado.Domain.Entities;

public class LinhaVenda
{
    public Guid Id { get; set; }
    public Guid VendaId { get; set; }
    public Guid ProdutoId { get; set; }

    /// <summary>Snapshot da descrição do produto no momento da venda (imutável).</summary>
    public string ProdutoDescricao { get; set; } = string.Empty;

    /// <summary>Snapshot do EAN-13 do produto no momento da venda.</summary>
    public string ProdutoEan13 { get; set; } = string.Empty;

    public int Quantidade { get; set; }

    /// <summary>Snapshot do preço unitário no momento da venda (não usa FK ao Produto.Preco).</summary>
    public decimal PrecoUnitario { get; set; }

    /// <summary>Taxa de IVA em percentagem (6, 13 ou 23) — snapshot da altura da venda.</summary>
    public int TaxaIvaPercentagem { get; set; }

    public decimal Subtotal { get; set; }
    public decimal ValorIva { get; set; }

    /// <summary>Quantidade já devolvida (UC02). 0 enquanto não há devoluções.</summary>
    public int QuantidadeDevolvida { get; set; }

    public Venda? Venda { get; set; }
    public Produto? Produto { get; set; }
}
