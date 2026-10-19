namespace BeloMercado.Domain.Entities;

public class Cliente
{
    public Guid Id { get; set; }
    public string Nif { get; set; } = string.Empty;
    public string? Nome { get; set; }
    public string? Email { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
