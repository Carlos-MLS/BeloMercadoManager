namespace BeloMercado.Domain.Entities;

public class LogAuditoria
{
    public Guid Id { get; set; }
    public Guid? UtilizadorId { get; set; }
    public string Acao { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = string.Empty;
    public string? TargetEntidade { get; set; }
    public Guid? TargetId { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public string? Detalhes { get; set; }
}
