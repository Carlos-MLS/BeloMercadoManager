using BeloMercado.Domain.Enums;

namespace BeloMercado.Domain.Entities;

public class Venda
{
    public Guid Id { get; set; }
    public Guid LojaId { get; set; }
    public Guid CaixaDiariaId { get; set; }
    public Guid OperadorId { get; set; }
    public Guid? ClienteId { get; set; }
    public DateTime DataHora { get; set; } = DateTime.UtcNow;
    public MetodoPagamento MetodoPagamento { get; set; }
    public EstadoVenda Estado { get; set; } = EstadoVenda.Concluida;
    public decimal? ValorRecebido { get; set; }
    public decimal? Troco { get; set; }
    public decimal TotalSemIva { get; set; }
    public decimal TotalIva { get; set; }
    public decimal Total { get; set; }

    public ICollection<LinhaVenda> Linhas { get; set; } = new List<LinhaVenda>();
    public Loja? Loja { get; set; }
    public Cliente? Cliente { get; set; }
    public CaixaDiaria? CaixaDiaria { get; set; }
}
