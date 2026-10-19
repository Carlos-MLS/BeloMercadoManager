namespace BeloMercado.Application.Dtos.Catalogo;

public class ProdutoResponse
{
    public Guid Id { get; set; }
    public string Ean13 { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public Guid CategoriaId { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public string TaxaIva { get; set; } = string.Empty;
    public int TaxaIvaPercentagem { get; set; }
    public string UnidadeMedida { get; set; } = string.Empty;
    public string? ImagemUrl { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
}
