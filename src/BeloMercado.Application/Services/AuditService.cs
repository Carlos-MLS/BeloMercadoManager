using BeloMercado.Application.Interfaces.Audit;
using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Application.Interfaces.Time;
using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Services;

public class AuditService : IAuditService
{
    private readonly ILogAuditoriaRepository _logRepository;
    private readonly IClock _clock;

    public AuditService(ILogAuditoriaRepository logRepository, IClock clock)
    {
        _logRepository = logRepository;
        _clock = clock;
    }

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
        var entry = new LogAuditoria
        {
            Id = Guid.NewGuid(),
            UtilizadorId = utilizadorId,
            Acao = acao,
            Timestamp = _clock.UtcNow,
            IpAddress = ipAddress,
            TargetEntidade = targetEntidade,
            TargetId = targetId,
            Resultado = resultado,
            Detalhes = detalhes
        };

        return _logRepository.AddAsync(entry, cancellationToken);
    }
}
