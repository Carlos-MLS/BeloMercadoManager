namespace BeloMercado.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UtilizadorId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public Utilizador? Utilizador { get; set; }

    public bool IsActive(DateTime now) =>
        RevokedAt is null && ExpiresAt > now;
}
