namespace BeloMercado.Application.Dtos.Dashboards;

public class KPIDiarioResponse
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
    public DateTime UltimaAtualizacao { get; set; }
}
