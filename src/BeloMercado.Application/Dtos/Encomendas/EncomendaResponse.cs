namespace BeloMercado.Application.Dtos.Encomendas;

public class EncomendaResponse
{
    public Guid Id { get; set; }
    public Guid FornecedorId { get; set; }
    public string FornecedorNome { get; set; } = string.Empty;
    public Guid LojaId { get; set; }
    public Guid CriadoPorId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataSubmissao { get; set; }
    public DateTime? DataRececao { get; set; }
    public decimal Total { get; set; }
    public List<LinhaEncomendaResponse> Linhas { get; set; } = new();
}
