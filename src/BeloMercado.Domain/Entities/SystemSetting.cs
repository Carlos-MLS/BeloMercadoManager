namespace BeloMercado.Domain.Entities;

/// <summary>
/// Key/value singleton para definições do sistema persistidas em BD.
/// Usado, por exemplo, para guardar o offset (em dias) do relógio aplicacional.
/// </summary>
public class SystemSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
