using BeloMercado.Application.Interfaces.Auth;

namespace BeloMercado.UnitTests.TestDoubles;

/// <summary>
/// "Hash" = "hashed:" + password. Verify devolve true se hash != null e
/// corresponder. Para hash == null/empty, comportamento deve manter
/// constant-time semântica simulada — devolve sempre false sem aceleração.
/// Pode ser configurado para devolver um resultado fixo.
/// </summary>
public class FakePasswordHasher : IPasswordHasher
{
    public bool? ForcedVerifyResult { get; set; }
    public int VerifyCallCount { get; private set; }

    public string Hash(string password) => "hashed:" + password;

    public bool Verify(string password, string? hash)
    {
        VerifyCallCount++;
        if (ForcedVerifyResult.HasValue)
        {
            return ForcedVerifyResult.Value;
        }
        if (string.IsNullOrEmpty(hash))
        {
            // Simula verify against dummy hash → resultado falso
            return false;
        }
        return hash == "hashed:" + password;
    }
}
