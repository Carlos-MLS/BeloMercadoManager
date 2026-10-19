using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Auth;
using BeloMercado.Application.Dtos.Me;
using BeloMercado.Application.Services;
using BeloMercado.Application.Settings;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using BeloMercado.UnitTests.TestDoubles;
using Microsoft.Extensions.Options;

namespace BeloMercado.UnitTests.Services;

/// <summary>
/// AuthAccessService — UC15 (autenticação) e UC17 (perfil próprio).
/// Regras-chave testadas:
///   RN01 Login falha sempre devolve genérico (cobertura via null result, sem fuga de info).
///   RN02 BCrypt verify é sempre chamado (mitigação timing-attack).
///   RN03 5 falhas consecutivas → BloqueadoAte = now + 15min.
///   RN04 Conta bloqueada não dá login mesmo com password correta.
///   Refresh: rotação correta + revogação do antigo.
///   Change password: precisa da atual; verifica complexidade; nova ≠ atual.
/// </summary>
public class AuthAccessServiceTests
{
    private static (AuthAccessService svc,
        InMemoryUserRepository users,
        InMemoryRefreshTokenRepository tokens,
        InMemoryLojaRepository lojas,
        FakePasswordHasher hasher,
        FakeTokenGenerator tokenGen,
        FakeAuditService audit,
        FakeUnitOfWork uow,
        FakeClock clock) Build(int maxFailedAttempts = 5, int lockoutMinutes = 15)
    {
        var users = new InMemoryUserRepository();
        var tokens = new InMemoryRefreshTokenRepository();
        var lojas = new InMemoryLojaRepository();
        var hasher = new FakePasswordHasher();
        var tokenGen = new FakeTokenGenerator();
        var audit = new FakeAuditService();
        var uow = new FakeUnitOfWork();
        var clock = new FakeClock();
        var authSettings = Options.Create(new AuthSettings
        {
            MaxFailedAttempts = maxFailedAttempts,
            LockoutMinutes = lockoutMinutes,
            BCryptCost = 11
        });
        var jwtSettings = Options.Create(new JwtSettings
        {
            Issuer = "test",
            Audience = "test",
            AccessTokenLifetimeMinutes = 15,
            RefreshTokenLifetimeDays = 7,
            Key = "x"
        });
        var svc = new AuthAccessService(users, tokens, lojas, hasher, tokenGen, audit, uow, authSettings, jwtSettings);
        return (svc, users, tokens, lojas, hasher, tokenGen, audit, uow, clock);
    }

    private static Utilizador SeedUser(InMemoryUserRepository users,
        string email = "operador@belomercado.pt",
        string password = "Password1!",
        Perfil perfil = Perfil.OperadorCaixa,
        bool ativo = true,
        Guid? lojaId = null,
        DateTime? bloqueadoAte = null)
    {
        var u = new Utilizador
        {
            Id = Guid.NewGuid(),
            Nome = "Op",
            Email = email,
            PasswordHash = "hashed:" + password,
            Perfil = perfil,
            LojaId = lojaId,
            Ativo = ativo,
            BloqueadoAte = bloqueadoAte
        };
        users.Users.Add(u);
        return u;
    }

    // ----------------- UC15 Login -----------------

    [Fact]
    public async Task Login_ComCredenciaisValidas_DevolveTokensEAtualizaUltimoLogin()
    {
        var (svc, users, tokens, _, _, _, audit, uow, _) = Build();
        var user = SeedUser(users);

        var resp = await svc.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "Password1!"
        }, "127.0.0.1");

        resp.Should().NotBeNull();
        resp!.AccessToken.Should().Be($"AT:{user.Id}");
        resp.RefreshToken.Should().StartWith("RT-");
        resp.Perfil.Should().Be(nameof(Perfil.OperadorCaixa));
        user.UltimoLogin.Should().NotBeNull();
        user.TentativasFalhadas.Should().Be(0);
        tokens.Tokens.Should().HaveCount(1);
        audit.Contains(AuditAcoes.LoginSucesso).Should().BeTrue();
        uow.SaveCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Login_PasswordIncorreta_DevolveNullEIncrementaTentativas()
    {
        var (svc, users, _, _, _, _, audit, _, _) = Build();
        var user = SeedUser(users);

        var resp = await svc.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "ERRADA"
        }, "127.0.0.1");

        resp.Should().BeNull();
        user.TentativasFalhadas.Should().Be(1);
        audit.Contains(AuditAcoes.LoginFalha).Should().BeTrue();
    }

    [Fact]
    public async Task Login_EmailDesconhecido_DevolveNull_SemRevelarExistencia_EChamaVerifyParaTimingConstant()
    {
        // RN02 — BCrypt.Verify deve ser chamado mesmo quando o user não existe
        // para evitar fuga por timing (user enumeration).
        var (svc, _, _, _, hasher, _, audit, _, _) = Build();

        var resp = await svc.LoginAsync(new LoginRequest
        {
            Email = "nao-existe@belomercado.pt",
            Password = "Whatever1!"
        }, "127.0.0.1");

        resp.Should().BeNull();
        hasher.VerifyCallCount.Should().Be(1);
        audit.Contains(AuditAcoes.LoginFalha).Should().BeTrue();
    }

    [Fact]
    public async Task Login_AoQuintaFalha_BloqueiaContaPor15Minutos()
    {
        // RN03
        var (svc, users, _, _, _, _, audit, _, clock) = Build(maxFailedAttempts: 5, lockoutMinutes: 15);
        var user = SeedUser(users);

        for (var i = 0; i < 5; i++)
        {
            await svc.LoginAsync(new LoginRequest
            {
                Email = user.Email,
                Password = "errada"
            }, "127.0.0.1");
        }

        user.BloqueadoAte.Should().NotBeNull();
        user.TentativasFalhadas.Should().Be(0);   // reset após bloqueio
        audit.Contains(AuditAcoes.ContaBloqueada).Should().BeTrue();
    }

    [Fact]
    public async Task Login_ContaInativa_NuncaAutentica()
    {
        var (svc, users, _, _, _, _, _, _, _) = Build();
        var user = SeedUser(users, ativo: false);

        var resp = await svc.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "Password1!"
        }, "127.0.0.1");

        resp.Should().BeNull();
    }

    [Fact]
    public async Task Login_ContaBloqueada_NaoAutenticaMesmoComPasswordCorrecta()
    {
        // RN04
        var (svc, users, _, _, _, _, _, _, _) = Build();
        var bloqueadoAte = DateTime.UtcNow.AddYears(10);
        SeedUser(users, bloqueadoAte: bloqueadoAte);

        var resp = await svc.LoginAsync(new LoginRequest
        {
            Email = "operador@belomercado.pt",
            Password = "Password1!"
        }, "127.0.0.1");

        resp.Should().BeNull();
    }

    // ----------------- Refresh -----------------

    [Fact]
    public async Task Refresh_TokenValido_RotacionaERevogaAntigo()
    {
        var (svc, users, tokens, _, _, _, audit, _, _) = Build();
        var user = SeedUser(users);
        var loginResp = await svc.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "Password1!"
        }, "127.0.0.1");

        var resp = await svc.RefreshAsync(new RefreshRequest
        {
            RefreshToken = loginResp!.RefreshToken
        }, "127.0.0.1");

        resp.Should().NotBeNull();
        resp!.RefreshToken.Should().NotBe(loginResp.RefreshToken);
        tokens.Tokens.Should().HaveCount(2);
        tokens.Tokens[0].RevokedAt.Should().NotBeNull();
        tokens.Tokens[0].ReplacedByTokenId.Should().Be(tokens.Tokens[1].Id);
        audit.Contains(AuditAcoes.TokenRefreshed).Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_TokenInexistente_DevolveNullEAuditFalha()
    {
        var (svc, _, _, _, _, _, audit, _, _) = Build();

        var resp = await svc.RefreshAsync(new RefreshRequest
        {
            RefreshToken = "lixo"
        }, "127.0.0.1");

        resp.Should().BeNull();
        audit.Contains(AuditAcoes.TokenRefreshFalha).Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_TokenRevogado_DevolveNull()
    {
        var (svc, users, tokens, _, _, _, _, _, _) = Build();
        var user = SeedUser(users);
        var login = await svc.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "Password1!"
        }, "127.0.0.1");
        tokens.Tokens[0].RevokedAt = DateTime.UtcNow;

        var resp = await svc.RefreshAsync(new RefreshRequest
        {
            RefreshToken = login!.RefreshToken
        }, "127.0.0.1");

        resp.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_TokenExpirado_DevolveNull()
    {
        var (svc, users, tokens, _, _, _, _, _, _) = Build();
        var user = SeedUser(users);
        var login = await svc.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "Password1!"
        }, "127.0.0.1");
        tokens.Tokens[0].ExpiresAt = DateTime.UtcNow.AddYears(-1);

        var resp = await svc.RefreshAsync(new RefreshRequest
        {
            RefreshToken = login!.RefreshToken
        }, "127.0.0.1");

        resp.Should().BeNull();
    }

    // ----------------- Logout -----------------

    [Fact]
    public async Task Logout_RevogaTokenERegistaAuditoria()
    {
        var (svc, users, tokens, _, _, _, audit, _, _) = Build();
        var user = SeedUser(users);
        var login = await svc.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "Password1!"
        }, "127.0.0.1");

        await svc.LogoutAsync(login!.RefreshToken, user.Id, "127.0.0.1");

        tokens.Tokens[0].RevokedAt.Should().NotBeNull();
        audit.Contains(AuditAcoes.LogoutSucesso).Should().BeTrue();
    }

    // ----------------- UC17 -----------------

    [Fact]
    public async Task GetMe_DevolvePerfilCompletoComLojaDesignacao()
    {
        var (svc, users, _, lojas, _, _, _, _, _) = Build();
        var loja = new Loja { Id = Guid.NewGuid(), Designacao = "Braga" };
        lojas.Lojas.Add(loja);
        var user = SeedUser(users, lojaId: loja.Id);

        var me = await svc.GetMeAsync(user.Id);

        me.Should().NotBeNull();
        me!.Nome.Should().Be("Op");
        me.LojaDesignacao.Should().Be("Braga");
    }

    [Fact]
    public async Task GetMe_UtilizadorInexistente_DevolveNull()
    {
        var (svc, _, _, _, _, _, _, _, _) = Build();
        var me = await svc.GetMeAsync(Guid.NewGuid());
        me.Should().BeNull();
    }

    [Fact]
    public async Task UpdateMe_AtualizaApenasNome_NaoMexeNoEmailNemPerfil()
    {
        var (svc, users, _, _, _, _, audit, _, _) = Build();
        var user = SeedUser(users);

        var resp = await svc.UpdateMeAsync(user.Id,
            new UpdateMeRequest { Nome = "Novo Nome" }, "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        user.Nome.Should().Be("Novo Nome");
        user.Email.Should().Be("operador@belomercado.pt");
        audit.Contains(AuditAcoes.PerfilAtualizado).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_ComPasswordAtualCerta_AltereiHashEFlagTemporaria()
    {
        var (svc, users, _, _, hasher, _, audit, _, _) = Build();
        var user = SeedUser(users);
        user.PasswordTemporaria = true;

        var resp = await svc.ChangePasswordAsync(user.Id,
            new ChangePasswordRequest { CurrentPassword = "Password1!", NewPassword = "Outra123!" },
            "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("hashed:Outra123!");
        user.PasswordTemporaria.Should().BeFalse();
        audit.Contains(AuditAcoes.PasswordAlterada).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_PasswordAtualErrada_DevolveValidationError()
    {
        var (svc, users, _, _, _, _, _, _, _) = Build();
        var user = SeedUser(users);

        var resp = await svc.ChangePasswordAsync(user.Id,
            new ChangePasswordRequest { CurrentPassword = "ERRADA", NewPassword = "Outra123!" },
            "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(PerfilErrorCodes.PasswordAtualIncorreta);
    }

    [Fact]
    public async Task ChangePassword_NovaPasswordFracaRecusada()
    {
        var (svc, users, _, _, _, _, _, _, _) = Build();
        var user = SeedUser(users);

        var resp = await svc.ChangePasswordAsync(user.Id,
            new ChangePasswordRequest { CurrentPassword = "Password1!", NewPassword = "abc" },
            "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(PerfilErrorCodes.PasswordComplexidadeInsuficiente);
    }

    [Fact]
    public async Task ChangePassword_NovaIgualAtualRecusada()
    {
        var (svc, users, _, _, _, _, _, _, _) = Build();
        var user = SeedUser(users);

        var resp = await svc.ChangePasswordAsync(user.Id,
            new ChangePasswordRequest { CurrentPassword = "Password1!", NewPassword = "Password1!" },
            "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(PerfilErrorCodes.PasswordIgualAtual);
    }
}
