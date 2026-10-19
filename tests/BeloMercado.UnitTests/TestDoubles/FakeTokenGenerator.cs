using BeloMercado.Application.Interfaces.Auth;
using BeloMercado.Domain.Entities;

namespace BeloMercado.UnitTests.TestDoubles;

/// <summary>
/// Gera tokens previsíveis para testes. AccessToken = "AT:" + userId.
/// RefreshToken = "RT-N" sequencial; hash = "H:" + rawToken.
/// </summary>
public class FakeTokenGenerator : ITokenGenerator
{
    private int _counter;

    public string GenerateAccessToken(Utilizador user) => $"AT:{user.Id}";

    public GeneratedRefreshToken GenerateRefreshToken()
    {
        _counter++;
        var raw = $"RT-{_counter}";
        return new GeneratedRefreshToken(raw, HashRefreshToken(raw));
    }

    public string HashRefreshToken(string rawToken) => "H:" + rawToken;
}
