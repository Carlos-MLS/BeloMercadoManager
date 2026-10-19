namespace BeloMercado.Domain.Entities;

public class Stock
{
    public Guid Id { get; set; }
    public Guid LojaId { get; set; }
    public Guid ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public int QtdMinima { get; set; }
    public int QtdMaxima { get; set; }

    public Loja? Loja { get; set; }
    public Produto? Produto { get; set; }
}
