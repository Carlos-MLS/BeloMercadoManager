namespace BeloMercado.Application.Dtos.Fornecedores;

public class FornecedorResponse
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Nif { get; set; } = string.Empty;
    public string Morada { get; set; } = string.Empty;
    public string Contacto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Ramo { get; set; }
    public string? CondicoesPagamento { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public List<ProdutoFornecedorResponse> ProdutosAssociados { get; set; } = new();
}
