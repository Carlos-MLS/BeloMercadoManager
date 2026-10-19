using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Interfaces.Auth;

public record GeneratedRefreshToken(string RawToken, string TokenHash);

public interface ITokenGenerator
{
    string GenerateAccessToken(Utilizador user);
    GeneratedRefreshToken GenerateRefreshToken();
    string HashRefreshToken(string rawToken);
}
