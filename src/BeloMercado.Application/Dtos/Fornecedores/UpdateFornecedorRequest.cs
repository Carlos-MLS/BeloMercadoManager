using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Fornecedores;

/// <summary>
/// Update payload for an existing supplier. NIF is intentionally NOT
/// editable here (UC09 alt 3a). To "delete" a supplier, set Ativo = false
/// (RN03 soft delete). Replaces the full set of associated products.
/// </summary>
public class UpdateFornecedorRequest
{
    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

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

    public bool Ativo { get; set; } = true;

    public List<ProdutoFornecedorRequest> ProdutosAssociados { get; set; } = new();
}
