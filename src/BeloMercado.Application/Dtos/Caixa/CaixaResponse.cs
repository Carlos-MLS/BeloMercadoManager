namespace BeloMercado.Application.Dtos.Caixa;

public class CaixaResponse
{
    public Guid Id { get; set; }
    public Guid LojaId { get; set; }
    public DateOnly Data { get; set; }
    public Guid OperadorAberturaId { get; set; }
    public DateTime DataHoraAbertura { get; set; }
    public Guid? OperadorFechoId { get; set; }
    public DateTime? DataHoraFecho { get; set; }
    public decimal FundoInicial { get; set; }
    public decimal? ValorEsperado { get; set; }
    public decimal? ValorContado { get; set; }
    public decimal? Diferenca { get; set; }
    public string? Justificacao { get; set; }
    public string Estado { get; set; } = string.Empty;
}
