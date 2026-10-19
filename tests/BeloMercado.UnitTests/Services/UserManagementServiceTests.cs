using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Utilizadores;
using BeloMercado.Application.Services;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using BeloMercado.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace BeloMercado.UnitTests.Services;

/// <summary>
/// UserManagementService — UC16 (Administração de Utilizadores). Regras:
///   RN16a Operador/Gerente exigem LojaId.
///   RN16b Administradores não podem ter LojaId.
///   RN16c Email único.
///   RN16d Uma loja só pode ter 1 Gerente ativo simultaneamente.
///   RN16e Senha temporária gerada com flag PasswordTemporaria=true.
/// </summary>
public class UserManagementServiceTests
{
    private static (UserManagementService svc,
        InMemoryUserRepository users,
        InMemoryLojaRepository lojas,
        FakePasswordHasher hasher,
        FakePasswordGenerator pwdGen,
        FakeAuditService audit,
        FakeUnitOfWork uow) Build()
    {
        var users = new InMemoryUserRepository();
        var lojas = new InMemoryLojaRepository();
        var hasher = new FakePasswordHasher();
        var pwdGen = new FakePasswordGenerator();
        var audit = new FakeAuditService();
        var uow = new FakeUnitOfWork();
        var clock = new FakeClock();
        var svc = new UserManagementService(users, lojas, hasher, pwdGen, audit, uow,
            NullLogger<UserManagementService>.Instance, clock);
        return (svc, users, lojas, hasher, pwdGen, audit, uow);
    }

    [Fact]
    public async Task Create_OperadorComLoja_CriaECoreNaPasswordTemporaria()
    {
        var (svc, users, lojas, _, _, audit, _) = Build();
        var loja = new Loja { Id = Guid.NewGuid(), Designacao = "Braga" };
        lojas.Lojas.Add(loja);

        var resp = await svc.CreateAsync(new CreateUtilizadorRequest
        {
            Nome = "Op",
            Email = "op@belomercado.pt",
            Perfil = Perfil.OperadorCaixa,
            LojaId = loja.Id
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        users.Users.Should().HaveCount(1);
        users.Users[0].PasswordTemporaria.Should().BeTrue();
        resp.Value!.TemporaryPassword.Should().NotBeNullOrEmpty();
        audit.Contains(AuditAcoes.UtilizadorCriado).Should().BeTrue();
    }

    [Fact]
    public async Task Create_OperadorSemLoja_Rejeita()
    {
        var (svc, _, _, _, _, _, _) = Build();

        var resp = await svc.CreateAsync(new CreateUtilizadorRequest
        {
            Nome = "Op",
            Email = "op@belomercado.pt",
            Perfil = Perfil.OperadorCaixa,
            LojaId = null
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(UtilizadoresErrorCodes.LojaObrigatoria);
    }

    [Fact]
    public async Task Create_AdministradorComLoja_Rejeita()
    {
        var (svc, _, _, _, _, _, _) = Build();

        var resp = await svc.CreateAsync(new CreateUtilizadorRequest
        {
            Nome = "Adm",
            Email = "adm@belomercado.pt",
            Perfil = Perfil.AdminIT,
            LojaId = Guid.NewGuid()
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(UtilizadoresErrorCodes.LojaNaoPermitida);
    }

    [Fact]
    public async Task Create_LojaInexistente_Rejeita()
    {
        var (svc, _, _, _, _, _, _) = Build();

        var resp = await svc.CreateAsync(new CreateUtilizadorRequest
        {
            Nome = "Op",
            Email = "op@belomercado.pt",
            Perfil = Perfil.OperadorCaixa,
            LojaId = Guid.NewGuid()
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(UtilizadoresErrorCodes.LojaInexistente);
    }

    [Fact]
    public async Task Create_EmailDuplicado_DevolveConflict()
    {
        var (svc, users, lojas, _, _, _, _) = Build();
        var loja = new Loja { Id = Guid.NewGuid() };
        lojas.Lojas.Add(loja);
        users.Users.Add(new Utilizador { Id = Guid.NewGuid(), Email = "op@belomercado.pt" });

        var resp = await svc.CreateAsync(new CreateUtilizadorRequest
        {
            Nome = "Op2",
            Email = "op@belomercado.pt",
            Perfil = Perfil.OperadorCaixa,
            LojaId = loja.Id
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(UtilizadoresErrorCodes.EmailDuplicado);
    }

    [Fact]
    public async Task Create_GerenteJaExisteNaLoja_DevolveConflict()
    {
        // RN16d
        var (svc, users, lojas, _, _, _, _) = Build();
        var loja = new Loja { Id = Guid.NewGuid() };
        lojas.Lojas.Add(loja);
        users.Users.Add(new Utilizador
        {
            Id = Guid.NewGuid(),
            Email = "gerente1@belomercado.pt",
            Perfil = Perfil.GerenteLoja,
            LojaId = loja.Id,
            Ativo = true
        });

        var resp = await svc.CreateAsync(new CreateUtilizadorRequest
        {
            Nome = "Gerente 2",
            Email = "gerente2@belomercado.pt",
            Perfil = Perfil.GerenteLoja,
            LojaId = loja.Id
        }, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(UtilizadoresErrorCodes.GerenteDuplicadoNaLoja);
    }

    [Fact]
    public async Task Update_DesativarUtilizador_RegistaAuditoriaDesativado()
    {
        var (svc, users, lojas, _, _, audit, _) = Build();
        var loja = new Loja { Id = Guid.NewGuid() };
        lojas.Lojas.Add(loja);
        var u = new Utilizador
        {
            Id = Guid.NewGuid(),
            Nome = "X",
            Email = "x@belomercado.pt",
            Perfil = Perfil.OperadorCaixa,
            LojaId = loja.Id,
            Ativo = true
        };
        users.Users.Add(u);

        await svc.UpdateAsync(u.Id, new UpdateUtilizadorRequest
        {
            Nome = "X",
            Perfil = Perfil.OperadorCaixa,
            LojaId = loja.Id,
            Ativo = false
        }, Guid.NewGuid(), "127.0.0.1");

        audit.Contains(AuditAcoes.UtilizadorDesativado).Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_HappyPath_DesativaERegista()
    {
        var (svc, users, _, _, _, audit, _) = Build();
        var u = new Utilizador { Id = Guid.NewGuid(), Ativo = true };
        users.Users.Add(u);

        var resp = await svc.DeactivateAsync(u.Id, Guid.NewGuid(), "127.0.0.1");

        resp.IsSuccess.Should().BeTrue();
        u.Ativo.Should().BeFalse();
        audit.Contains(AuditAcoes.UtilizadorDesativado).Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_NaoEncontrado_DevolveNotFound()
    {
        var (svc, _, _, _, _, _, _) = Build();
        var resp = await svc.DeactivateAsync(Guid.NewGuid(), Guid.NewGuid(), "127.0.0.1");
        resp.IsSuccess.Should().BeFalse();
        resp.ErrorCode.Should().Be(UtilizadoresErrorCodes.NaoEncontrado);
    }
}
