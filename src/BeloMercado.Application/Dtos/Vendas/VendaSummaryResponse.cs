namespace BeloMercado.Application.Dtos.Vendas;

/// <summary>
/// Versão sumária de uma venda, usada para listagens (ex.: vendas
/// de uma caixa para o ecrã de anular/devolver). Não inclui linhas.
/// </summary>
public class VendaSummaryResponse
{
    public Guid Id { get; set; }
    public DateTime DataHora { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string MetodoPagamento { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string? FaturaNumero { get; set; }
    public string? ClienteNif { get; set; }
    public int NumLinhas { get; set; }
    public bool TemDevolucoes { get; set; }
}
