namespace BeloMercado.Application.Dtos.Vendas;

public class FaturaResponse
{
    public Guid Id { get; set; }
    public string NumeroCompleto { get; set; } = string.Empty;
    public DateTime DataEmissao { get; set; }
    public string? ClienteNif { get; set; }
    public string? ClienteNome { get; set; }
    public decimal TotalSemIva { get; set; }
    public decimal TotalIva { get; set; }
    public decimal Total { get; set; }
}
