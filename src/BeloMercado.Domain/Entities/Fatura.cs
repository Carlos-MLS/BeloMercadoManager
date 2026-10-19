namespace BeloMercado.Domain.Entities;

public class Fatura
{
    public Guid Id { get; set; }
    public Guid VendaId { get; set; }
    public Guid LojaId { get; set; }
    public int Ano { get; set; }
    public int NumeroSequencial { get; set; }

    /// <summary>Número de fatura legível, sequencial por loja e ano.</summary>
    public string NumeroCompleto { get; set; } = string.Empty;

    public DateTime DataEmissao { get; set; } = DateTime.UtcNow;
    public Guid? ClienteId { get; set; }
    public decimal TotalSemIva { get; set; }
    public decimal TotalIva { get; set; }
    public decimal Total { get; set; }

    public Venda? Venda { get; set; }
    public Cliente? Cliente { get; set; }
}
