namespace BeloMercado.Domain.Entities;

public class HistoricoPreco
{
    public Guid Id { get; set; }
    public Guid ProdutoId { get; set; }
    public decimal Preco { get; set; }
    public DateTime DataMudanca { get; set; } = DateTime.UtcNow;
    public Guid? AlteradoPorUtilizadorId { get; set; }

    public Produto? Produto { get; set; }
}
