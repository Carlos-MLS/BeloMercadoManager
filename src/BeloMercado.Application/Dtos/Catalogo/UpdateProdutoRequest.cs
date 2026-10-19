using System.ComponentModel.DataAnnotations;
using BeloMercado.Domain.Enums;

namespace BeloMercado.Application.Dtos.Catalogo;

public class UpdateProdutoRequest
{
    [Required, MaxLength(255)]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    public Guid CategoriaId { get; set; }

    [Range(0.01, 99999.99)]
    public decimal Preco { get; set; }

    [Required]
    public TaxaIva TaxaIva { get; set; }

    [Required, MaxLength(16)]
    public string UnidadeMedida { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? ImagemUrl { get; set; }

    public bool Ativo { get; set; } = true;
}
