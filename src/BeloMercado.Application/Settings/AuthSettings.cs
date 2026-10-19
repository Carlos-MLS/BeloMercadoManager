namespace BeloMercado.Application.Settings;

public class AuthSettings
{
    public const string SectionName = "Auth";

    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int BCryptCost { get; set; } = 11;
}
