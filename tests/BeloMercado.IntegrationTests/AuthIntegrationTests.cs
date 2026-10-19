using BeloMercado.Application.Dtos.Auth;
using BeloMercado.Application.Services;
using BeloMercado.Application.Settings;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using BeloMercado.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Options;

namespace BeloMercado.IntegrationTests;

/// <summary>
/// UC15 + UC17 contra repositórios reais. Foca o caminho onde EF Core
/// faz tracking — login → refresh → logout deixa o token original Revoked
/// na BD e cria um novo.
/// </summary>
public class AuthIntegrationTests
{
    [Fact]
    public async Task LoginAndRefresh_RevogaAntigoEMantemNovoNaBD()
    {
        using var f = new IntegrationTestFixture();
        // Seed user diretamente no DbContext
        var user = new Utilizador
        {
            Id = Guid.NewGuid(),
            Nome = "Op",
            Email = "op@belomercado.pt",
            PasswordHash = "hashed:Password1!",
            Perfil = Perfil.OperadorCaixa,
            Ativo = true
        };
        f.Db.Utilizadores.Add(user);
        await f.Db.SaveChangesAsync();

        var auth = BuildAuth(f);

        var login = await auth.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "Password1!"
        }, "127.0.0.1");
        login.Should().NotBeNull();

        var refresh = await auth.RefreshAsync(new RefreshRequest
        {
            RefreshToken = login!.RefreshToken
        }, "127.0.0.1");
        refresh.Should().NotBeNull();

        f.Db.RefreshTokens.Should().HaveCount(2);
        f.Db.RefreshTokens.Where(t => t.RevokedAt != null).Should().HaveCount(1);
        f.Db.RefreshTokens.Where(t => t.RevokedAt == null).Should().HaveCount(1);
    }

    [Fact]
    public async Task CincoFalhas_BloqueiamContaEPersistemEstado()
    {
        using var f = new IntegrationTestFixture();
        var user = new Utilizador
        {
            Id = Guid.NewGuid(),
            Email = "op@belomercado.pt",
            PasswordHash = "hashed:CertoPwd1!",
            Perfil = Perfil.OperadorCaixa,
            Ativo = true
        };
        f.Db.Utilizadores.Add(user);
        await f.Db.SaveChangesAsync();
        var auth = BuildAuth(f);

        for (var i = 0; i < 5; i++)
        {
            await auth.LoginAsync(new LoginRequest
            {
                Email = user.Email,
                Password = "Errada1!"
            }, "127.0.0.1");
        }

        var reloaded = f.Db.Utilizadores.Single(u => u.Id == user.Id);
        reloaded.BloqueadoAte.Should().NotBeNull();
    }

    private static AuthAccessService BuildAuth(IntegrationTestFixture f)
    {
        var authSettings = Options.Create(new AuthSettings { MaxFailedAttempts = 5, LockoutMinutes = 15, BCryptCost = 11 });
        var jwtSettings = Options.Create(new JwtSettings { Issuer = "t", Audience = "t", AccessTokenLifetimeMinutes = 15, RefreshTokenLifetimeDays = 7, Key = "x" });
        return new AuthAccessService(
            f.UserRepo, f.RefreshTokenRepo, f.LojaRepo, f.Hasher, f.Tokens,
            f.Audit, f.UnitOfWork, authSettings, jwtSettings);
    }
}
