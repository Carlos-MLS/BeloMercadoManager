using BeloMercado.Application.Dtos.Auth;
using BeloMercado.Application.Services;
using BeloMercado.Application.Settings;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using BeloMercado.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Options;

namespace BeloMercado.AcceptanceTests.Acceptance;

/// <summary>
/// UC15 — Autenticar Utilizador (login + bloqueio + refresh).
/// Critérios essenciais (SRS RNF-Segurança):
///   * Login com credenciais válidas devolve par AccessToken/RefreshToken.
///   * 5 falhas seguidas bloqueiam a conta por 15 minutos.
///   * Email inexistente é indistinguível de password incorreta para o cliente
///     (mitigação de user enumeration).
///   * Refresh com token revogado é recusado.
/// </summary>
public class UC15_AutenticarTests
{
    [Fact]
    public async Task UC15_Principal_LoginComCredenciaisValidas_DevolveTokens()
    {
        using var f = new IntegrationTestFixture();
        var user = SeedUser(f, password: "Password1!");
        var auth = BuildAuth(f);

        var resp = await auth.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "Password1!"
        }, "127.0.0.1");

        resp.Should().NotBeNull();
        resp!.AccessToken.Should().StartWith("AT:");
        resp.RefreshToken.Should().StartWith("RT-");
        resp.Perfil.Should().Be(nameof(Perfil.OperadorCaixa));
    }

    [Fact]
    public async Task UC15_Alt1_PasswordErrada_DevolveNullSemRevelarSeOEmailExiste()
    {
        using var f = new IntegrationTestFixture();
        SeedUser(f);
        var auth = BuildAuth(f);

        var existente = await auth.LoginAsync(new LoginRequest
        {
            Email = "op@belomercado.pt",
            Password = "ERRADA"
        }, "127.0.0.1");
        var inexistente = await auth.LoginAsync(new LoginRequest
        {
            Email = "naoexiste@belomercado.pt",
            Password = "ERRADA"
        }, "127.0.0.1");

        existente.Should().BeNull();
        inexistente.Should().BeNull();
        // Não há forma de distinguir cliente — comportamento idêntico (UC15 RN02).
    }

    [Fact]
    public async Task UC15_Alt2_CincoFalhas_BloqueiamContaEDecisaoNaoSeAltera()
    {
        using var f = new IntegrationTestFixture();
        var user = SeedUser(f, password: "Password1!");
        var auth = BuildAuth(f);

        for (var i = 0; i < 5; i++)
        {
            await auth.LoginAsync(new LoginRequest { Email = user.Email, Password = "x" }, "127.0.0.1");
        }
        // mesmo com password certa, conta está bloqueada
        var resp = await auth.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "Password1!"
        }, "127.0.0.1");

        resp.Should().BeNull();
        var reloaded = f.Db.Utilizadores.Single(u => u.Id == user.Id);
        reloaded.BloqueadoAte.Should().NotBeNull();
    }

    [Fact]
    public async Task UC15_Refresh_RotacionaTokenERevogaAntigo()
    {
        using var f = new IntegrationTestFixture();
        var user = SeedUser(f);
        var auth = BuildAuth(f);
        var login = await auth.LoginAsync(new LoginRequest { Email = user.Email, Password = "Password1!" }, "127.0.0.1");

        var refresh = await auth.RefreshAsync(new RefreshRequest { RefreshToken = login!.RefreshToken }, "127.0.0.1");

        refresh.Should().NotBeNull();
        refresh!.RefreshToken.Should().NotBe(login.RefreshToken);
        f.Db.RefreshTokens.Count(t => t.RevokedAt == null).Should().Be(1);
    }

    private static Utilizador SeedUser(IntegrationTestFixture f, string password = "Password1!")
    {
        var u = new Utilizador
        {
            Id = Guid.NewGuid(),
            Nome = "Op",
            Email = "op@belomercado.pt",
            PasswordHash = "hashed:" + password,
            Perfil = Perfil.OperadorCaixa,
            Ativo = true
        };
        f.Db.Utilizadores.Add(u);
        f.Db.SaveChanges();
        return u;
    }

    private static AuthAccessService BuildAuth(IntegrationTestFixture f)
    {
        var authSettings = Options.Create(new AuthSettings { MaxFailedAttempts = 5, LockoutMinutes = 15, BCryptCost = 11 });
        var jwtSettings = Options.Create(new JwtSettings { Issuer = "t", Audience = "t", AccessTokenLifetimeMinutes = 15, RefreshTokenLifetimeDays = 7, Key = "x" });
        return new AuthAccessService(f.UserRepo, f.RefreshTokenRepo, f.LojaRepo,
            f.Hasher, f.Tokens, f.Audit, f.UnitOfWork, authSettings, jwtSettings);
    }
}
