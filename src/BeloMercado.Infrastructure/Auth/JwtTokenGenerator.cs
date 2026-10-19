using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Interfaces.Auth;
using BeloMercado.Application.Settings;
using BeloMercado.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BeloMercado.Infrastructure.Auth;

public class JwtTokenGenerator : ITokenGenerator
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateAccessToken(Utilizador user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwtSettings.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new(JwtClaimNames.Role, user.Perfil.ToString()),
            new(JwtClaimNames.Name, user.Nome),
            new(JwtClaimNames.Email, user.Email)
        };

        if (user.LojaId.HasValue)
        {
            claims.Add(new Claim(JwtClaimNames.LojaId, user.LojaId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public GeneratedRefreshToken GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(bytes);
        var hash = HashRefreshToken(rawToken);
        return new GeneratedRefreshToken(rawToken, hash);
    }

    public string HashRefreshToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
