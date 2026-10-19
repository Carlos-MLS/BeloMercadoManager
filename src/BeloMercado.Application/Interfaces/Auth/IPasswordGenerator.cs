namespace BeloMercado.Application.Interfaces.Auth;

public interface IPasswordGenerator
{
    /// <summary>
    /// Generates a temporary password meeting the BCrypt user policy
    /// (>= 12 chars, includes uppercase, lowercase, digit and symbol).
    /// </summary>
    string GenerateTemporaryPassword();
}
