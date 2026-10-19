namespace BeloMercado.Domain.Entities;

public class Fornecedor
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Nif { get; set; } = string.Empty;
    public string Morada { get; set; } = string.Empty;
    public string Contacto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Ramo { get; set; }
    public string? CondicoesPagamento { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public ICollection<ProdutoFornecedor> ProdutosAssociados { get; set; } = new List<ProdutoFornecedor>();
}
