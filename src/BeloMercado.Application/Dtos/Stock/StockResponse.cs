namespace BeloMercado.Application.Dtos.Stock;

public class StockResponse
{
    public Guid Id { get; set; }
    public Guid LojaId { get; set; }
    public string LojaDesignacao { get; set; } = string.Empty;
    public Guid ProdutoId { get; set; }
    public string ProdutoEan13 { get; set; } = string.Empty;
    public string ProdutoDescricao { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public int QtdMinima { get; set; }
    public int QtdMaxima { get; set; }
    public bool AbaixoMinimo { get; set; }
}
