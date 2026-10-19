namespace BeloMercado.Domain.Entities;

public class Categoria
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid? ParentCategoriaId { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public Categoria? Parent { get; set; }
}
