using BeloMercado.Domain.Enums;

namespace BeloMercado.Domain.Entities;

public class Produto
{
    public Guid Id { get; set; }
    public string Ean13 { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public Guid CategoriaId { get; set; }
    public decimal Preco { get; set; }
    public TaxaIva TaxaIva { get; set; }
    public string UnidadeMedida { get; set; } = string.Empty;
    public string? ImagemUrl { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public Categoria? Categoria { get; set; }
}
