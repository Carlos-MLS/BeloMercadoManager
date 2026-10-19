namespace BeloMercado.Application.Dtos.Catalogo;

public class CategoriaResponse
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid? ParentCategoriaId { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
}
