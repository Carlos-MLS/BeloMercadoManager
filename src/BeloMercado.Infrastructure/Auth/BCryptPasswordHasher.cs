using BeloMercado.Application.Interfaces.Auth;
using BeloMercado.Application.Settings;
using Microsoft.Extensions.Options;

namespace BeloMercado.Infrastructure.Auth;

public class BCryptPasswordHasher : IPasswordHasher
{
    private readonly AuthSettings _authSettings;

    // Lazily computed once: a real BCrypt hash that Verify can run against
    // when no stored hash exists, so timing stays constant.
    private readonly Lazy<string> _dummyHash;

    public BCryptPasswordHasher(IOptions<AuthSettings> authSettings)
    {
        _authSettings = authSettings.Value;
        _dummyHash = new Lazy<string>(() =>
            BCrypt.Net.BCrypt.HashPassword("dummy", _authSettings.BCryptCost));
    }

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, _authSettings.BCryptCost);

    public bool Verify(string password, string? hash)
    {
        var effectiveHash = string.IsNullOrEmpty(hash) ? _dummyHash.Value : hash;
        try
        {
            var result = BCrypt.Net.BCrypt.Verify(password, effectiveHash);
            // When falling back to the dummy hash we must always return false,
            // regardless of what the dummy compared to.
            return !string.IsNullOrEmpty(hash) && result;
        }
        catch
        {
            // Malformed hash (e.g., legacy format) — treat as failed verify.
            return false;
        }
    }
}
