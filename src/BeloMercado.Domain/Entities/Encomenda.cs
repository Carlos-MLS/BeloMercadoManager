using BeloMercado.Domain.Enums;

namespace BeloMercado.Domain.Entities;

public class Encomenda
{
    public Guid Id { get; set; }
    public Guid FornecedorId { get; set; }
    public Guid LojaId { get; set; }
    public Guid CriadoPorId { get; set; }
    public EstadoEncomenda Estado { get; set; } = EstadoEncomenda.Rascunho;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataSubmissao { get; set; }
    public DateTime? DataRececao { get; set; }
    public decimal Total { get; set; }

    public ICollection<LinhaEncomenda> Linhas { get; set; } = new List<LinhaEncomenda>();
    public Fornecedor? Fornecedor { get; set; }
    public Loja? Loja { get; set; }
}
