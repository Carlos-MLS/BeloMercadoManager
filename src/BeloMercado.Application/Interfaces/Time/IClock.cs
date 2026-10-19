namespace BeloMercado.Application.Interfaces.Time;

/// <summary>
/// Relógio aplicacional. Permite injectar um offset (em dias) para suportar
/// avanço temporal em ambiente de demonstração, sem mexer no relógio do SO.
/// Não é usado para timestamps de auth/JWT — esses ficam-se pelo relógio real.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
    DateOnly Today { get; }
    int OffsetDays { get; }
    Task SetOffsetAsync(int offsetDays, CancellationToken cancellationToken = default);
    Task ShiftAsync(int deltaDays, CancellationToken cancellationToken = default);
}
