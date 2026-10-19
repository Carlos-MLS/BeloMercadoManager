namespace BeloMercado.Application.Interfaces.Auth;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Verifies a password against a stored hash. When <paramref name="hash"/>
    /// is null or empty, the implementation must still perform a verification
    /// against an internal dummy hash so timing remains constant
    /// (mitigates user-enumeration via timing).
    /// </summary>
    bool Verify(string password, string? hash);
}
