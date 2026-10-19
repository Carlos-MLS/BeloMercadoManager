using BeloMercado.Application.Interfaces.Audit;

namespace BeloMercado.UnitTests.TestDoubles;

/// <summary>
/// Captura todos os eventos de auditoria emitidos pelos serviços para que
/// os testes possam verificar transversalmente que cada operação write
/// regista uma entrada (regra: <c>AuditService.LogAsync</c> é obrigatório
/// em todas as operações de escrita).
/// </summary>
public class FakeAuditService : IAuditService
{
    public List<AuditEntry> Entries { get; } = new();

    public Task LogAsync(
        string acao,
        string resultado,
        Guid? utilizadorId,
        string ipAddress,
        string? targetEntidade = null,
        Guid? targetId = null,
        string? detalhes = null,
        CancellationToken cancellationToken = default)
    {
        Entries.Add(new AuditEntry(
            acao, resultado, utilizadorId, ipAddress, targetEntidade, targetId, detalhes));
        return Task.CompletedTask;
    }

    public bool Contains(string acao) => Entries.Any(e => e.Acao == acao);
    public AuditEntry? Last(string acao) => Entries.LastOrDefault(e => e.Acao == acao);
}

public record AuditEntry(
    string Acao,
    string Resultado,
    Guid? UtilizadorId,
    string IpAddress,
    string? TargetEntidade,
    Guid? TargetId,
    string? Detalhes);
