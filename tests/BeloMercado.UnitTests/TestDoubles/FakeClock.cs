using BeloMercado.Application.Interfaces.Time;

namespace BeloMercado.UnitTests.TestDoubles;

/// <summary>
/// Implementação de <see cref="IClock"/> com tempo manualmente controlado.
/// Usado por testes para tornar determinístico o comportamento de regras
/// dependentes de "agora" (e.g. expiração de lockout, idade de venda).
/// </summary>
public class FakeClock : IClock
{
    public DateTime UtcNow { get; set; }
    public DateTime Now => UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(UtcNow.Date);
    public int OffsetDays { get; set; }

    public FakeClock(DateTime? now = null)
    {
        UtcNow = now ?? new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
    }

    public Task SetOffsetAsync(int offsetDays, CancellationToken cancellationToken = default)
    {
        OffsetDays = offsetDays;
        return Task.CompletedTask;
    }

    public Task ShiftAsync(int deltaDays, CancellationToken cancellationToken = default)
    {
        OffsetDays += deltaDays;
        UtcNow = UtcNow.AddDays(deltaDays);
        return Task.CompletedTask;
    }
}
