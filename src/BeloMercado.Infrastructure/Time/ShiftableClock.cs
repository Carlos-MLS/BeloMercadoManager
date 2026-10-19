using BeloMercado.Application.Interfaces.Time;
using BeloMercado.Domain.Entities;
using BeloMercado.Infrastructure.Persistence;
using BeloMercado.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BeloMercado.Infrastructure.Time;

/// <summary>
/// Relógio aplicacional com offset configurável em dias. O offset é persistido
/// na tabela SystemSettings (chave "TimeOffsetDays") e mantido em memória entre
/// chamadas. O endpoint /dev/avancar-dias actualiza-o em tempo de execução.
/// </summary>
public class ShiftableClock : IClock
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShiftableClock> _logger;
    private int _offsetDays;
    private bool _initialized;
    private readonly object _gate = new();

    public ShiftableClock(IServiceScopeFactory scopeFactory, ILogger<ShiftableClock> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public int OffsetDays
    {
        get
        {
            EnsureInitialized();
            return _offsetDays;
        }
    }

    public DateTime UtcNow
    {
        get
        {
            EnsureInitialized();
            return DateTime.UtcNow.AddDays(_offsetDays);
        }
    }

    public DateTime Now
    {
        get
        {
            EnsureInitialized();
            return DateTime.Now.AddDays(_offsetDays);
        }
    }

    public DateOnly Today => DateOnly.FromDateTime(Now);

    public async Task SetOffsetAsync(int offsetDays, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BeloMercadoDbContext>();

        var setting = await db.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == SystemSettingConfiguration.TimeOffsetDaysKey, cancellationToken);
        if (setting is null)
        {
            setting = new SystemSetting
            {
                Key = SystemSettingConfiguration.TimeOffsetDaysKey,
                Value = offsetDays.ToString()
            };
            db.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = offsetDays.ToString();
        }
        await db.SaveChangesAsync(cancellationToken);

        lock (_gate)
        {
            _offsetDays = offsetDays;
            _initialized = true;
        }
        _logger.LogInformation("ShiftableClock offset = {Offset} dias", offsetDays);
    }

    public Task ShiftAsync(int deltaDays, CancellationToken cancellationToken = default)
        => SetOffsetAsync(OffsetDays + deltaDays, cancellationToken);

    private void EnsureInitialized()
    {
        if (_initialized) return;
        lock (_gate)
        {
            if (_initialized) return;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BeloMercadoDbContext>();
                var raw = db.SystemSettings
                    .Where(s => s.Key == SystemSettingConfiguration.TimeOffsetDaysKey)
                    .Select(s => s.Value)
                    .FirstOrDefault();
                _offsetDays = int.TryParse(raw, out var v) ? v : 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "ShiftableClock: falha a ler offset da BD; a usar 0 por defeito.");
                _offsetDays = 0;
            }
            _initialized = true;
        }
    }
}
