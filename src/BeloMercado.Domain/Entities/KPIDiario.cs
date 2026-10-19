namespace BeloMercado.Domain.Entities;

/// <summary>
/// Tabela de leitura denormalizada — agregados de uma loja para um dia.
/// Materializada pelo ConsolidationService (UC12). Lida pelo Dashboard
/// de KPIs (UC13). One row per (LojaId, Data).
/// </summary>
public class KPIDiario
{
    public Guid LojaId { get; set; }
    public DateOnly Data { get; set; }

    public decimal TotalVendas { get; set; }
    public int NumTransacoes { get; set; }
    public decimal TicketMedio { get; set; }
    public decimal TotalIva { get; set; }
    public decimal TotalDescontos { get; set; }
    public int VendasAnuladas { get; set; }
    public int Devolucoes { get; set; }

    public DateTime UltimaAtualizacao { get; set; } = DateTime.UtcNow;

    public Loja? Loja { get; set; }
}
