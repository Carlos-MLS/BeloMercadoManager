namespace BeloMercado.Application.Dtos.Lojas;

public class LojaResponse
{
    public Guid Id { get; set; }
    public string Designacao { get; set; } = string.Empty;
    public string Morada { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Contacto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Horario { get; set; }
    public int StockMinPorDefeito { get; set; }
    public int StockMaxPorDefeito { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
}
