using BeloMercado.Domain.Enums;

namespace BeloMercado.Domain.Entities;

/// <summary>
/// Registo de uma execução do ConsolidationService para uma (loja, data).
/// ADR-006: a topologia centralizada eliminou o atributo HashIntegridade
/// (origem do payload distribuído), por isso só restam estes campos.
/// </summary>
public class ConsolidacaoDiaria
{
    public Guid Id { get; set; }
    public Guid LojaId { get; set; }
    public DateOnly Data { get; set; }
    public EstadoConsolidacao Estado { get; set; } = EstadoConsolidacao.Pendente;
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public int Tentativas { get; set; }
    public string? UltimaMensagemErro { get; set; }

    /// <summary>"Imediata" (após fecho de caixa) ou "Agendada" (cron 23h30).</summary>
    public string Origem { get; set; } = string.Empty;

    public Loja? Loja { get; set; }
}
