using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Fornecedores;

public class CreateFornecedorRequest
{
    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required, StringLength(9, MinimumLength = 9)]
    public string Nif { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Morada { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string Contacto { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? Ramo { get; set; }

    [MaxLength(255)]
    public string? CondicoesPagamento { get; set; }

    public List<ProdutoFornecedorRequest> ProdutosAssociados { get; set; } = new();
}
