namespace BeloMercado.Application.Dtos.Promocoes;

public class PromocaoResponse
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid ProdutoId { get; set; }
    public string ProdutoEan13 { get; set; } = string.Empty;
    public string ProdutoDescricao { get; set; } = string.Empty;
    public Guid? LojaId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
}
