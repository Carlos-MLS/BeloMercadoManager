namespace BeloMercado.Application.Dtos.Vendas;

public class VendaResponse
{
    public Guid Id { get; set; }
    public Guid LojaId { get; set; }
    public Guid CaixaDiariaId { get; set; }
    public Guid OperadorId { get; set; }
    public DateTime DataHora { get; set; }
    public string MetodoPagamento { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal? ValorRecebido { get; set; }
    public decimal? Troco { get; set; }
    public decimal TotalSemIva { get; set; }
    public decimal TotalIva { get; set; }
    public decimal Total { get; set; }

    public List<LinhaVendaResponse> Linhas { get; set; } = new();
    public FaturaResponse Fatura { get; set; } = new();
}
