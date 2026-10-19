namespace BeloMercado.Domain.Entities;

public class IdempotencyEntry
{
    public string Key { get; set; } = string.Empty;
    public Guid VendaId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
