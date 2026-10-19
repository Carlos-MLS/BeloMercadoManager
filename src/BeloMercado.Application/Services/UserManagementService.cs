using BeloMercado.Application.Common;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Utilizadores;
using BeloMercado.Application.Interfaces.Audit;
using BeloMercado.Application.Interfaces.Auth;
using BeloMercado.Application.Interfaces.Persistence;
using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Application.Interfaces.Time;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BeloMercado.Application.Services;

public class UserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly ILojaRepository _lojaRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordGenerator _passwordGenerator;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserManagementService> _logger;
    private readonly IClock _clock;

    public UserManagementService(
        IUserRepository userRepository,
        ILojaRepository lojaRepository,
        IPasswordHasher passwordHasher,
        IPasswordGenerator passwordGenerator,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<UserManagementService> logger,
        IClock clock)
    {
        _userRepository = userRepository;
        _lojaRepository = lojaRepository;
        _passwordHasher = passwordHasher;
        _passwordGenerator = passwordGenerator;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _clock = clock;
    }

    public async Task<IReadOnlyList<UtilizadorResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(MapToResponse).ToList();
    }

    public async Task<UtilizadorResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : MapToResponse(user);
    }

    public async Task<OperationResult<CreateUtilizadorResponse>> CreateAsync(
        CreateUtilizadorRequest request,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidatePerfilLojaCombinationAsync(
            request.Perfil, request.LojaId, cancellationToken);
        if (validationError is not null)
        {
            return OperationResult<CreateUtilizadorResponse>.Validation(validationError.Value.code, validationError.Value.message);
        }

        if (await _userRepository.EmailExistsAsync(request.Email, excludeId: null, cancellationToken))
        {
            return OperationResult<CreateUtilizadorResponse>.Conflict(
                UtilizadoresErrorCodes.EmailDuplicado,
                "Já existe um utilizador com esse email.");
        }

        if (request.Perfil == Perfil.GerenteLoja
            && request.LojaId is not null
            && await _userRepository.ActiveGerenteExistsForLojaAsync(request.LojaId.Value, excludeId: null, cancellationToken))
        {
            return OperationResult<CreateUtilizadorResponse>.Conflict(
                UtilizadoresErrorCodes.GerenteDuplicadoNaLoja,
                "Já existe um Gerente ativo nessa loja.");
        }

        var temporaryPassword = _passwordGenerator.GenerateTemporaryPassword();
        var utilizador = new Utilizador
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(temporaryPassword),
            PasswordTemporaria = true,
            Perfil = request.Perfil,
            LojaId = request.LojaId,
            Ativo = true,
            DataCriacao = _clock.UtcNow,
            TentativasFalhadas = 0
        };

        await _userRepository.AddAsync(utilizador, cancellationToken);
        await _auditService.LogAsync(
            AuditAcoes.UtilizadorCriado,
            AuditResultados.Sucesso,
            actorId,
            ipAddress,
            targetEntidade: nameof(Utilizador),
            targetId: utilizador.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // No email integration in the academic prototype — log the credential
        // so the developer/admin can read it from console output.
        _logger.LogInformation(
            "Utilizador criado: {Email} (perfil={Perfil}, lojaId={LojaId}). Password temporária: {TempPassword}",
            utilizador.Email, utilizador.Perfil, utilizador.LojaId, temporaryPassword);

        return OperationResult<CreateUtilizadorResponse>.Ok(new CreateUtilizadorResponse
        {
            Utilizador = MapToResponse(utilizador),
            TemporaryPassword = temporaryPassword
        });
    }

    public async Task<OperationResult<UtilizadorResponse>> UpdateAsync(
        Guid id,
        UpdateUtilizadorRequest request,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var utilizador = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (utilizador is null)
        {
            return OperationResult<UtilizadorResponse>.NotFound(
                UtilizadoresErrorCodes.NaoEncontrado,
                "Utilizador não encontrado.");
        }

        var validationError = await ValidatePerfilLojaCombinationAsync(
            request.Perfil, request.LojaId, cancellationToken);
        if (validationError is not null)
        {
            return OperationResult<UtilizadorResponse>.Validation(validationError.Value.code, validationError.Value.message);
        }

        if (request.Perfil == Perfil.GerenteLoja
            && request.LojaId is not null
            && request.Ativo
            && await _userRepository.ActiveGerenteExistsForLojaAsync(request.LojaId.Value, excludeId: id, cancellationToken))
        {
            return OperationResult<UtilizadorResponse>.Conflict(
                UtilizadoresErrorCodes.GerenteDuplicadoNaLoja,
                "Já existe um Gerente ativo nessa loja.");
        }

        var ativoAnterior = utilizador.Ativo;

        utilizador.Nome = request.Nome;
        utilizador.Perfil = request.Perfil;
        utilizador.LojaId = request.LojaId;
        utilizador.Ativo = request.Ativo;

        var acao = (ativoAnterior, request.Ativo) switch
        {
            (true, false) => AuditAcoes.UtilizadorDesativado,
            (false, true) => AuditAcoes.UtilizadorReativado,
            _ => AuditAcoes.UtilizadorAtualizado
        };

        await _auditService.LogAsync(
            acao,
            AuditResultados.Sucesso,
            actorId,
            ipAddress,
            targetEntidade: nameof(Utilizador),
            targetId: utilizador.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<UtilizadorResponse>.Ok(MapToResponse(utilizador));
    }

    public async Task<OperationResult<UtilizadorResponse>> DeactivateAsync(
        Guid id,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var utilizador = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (utilizador is null)
        {
            return OperationResult<UtilizadorResponse>.NotFound(
                UtilizadoresErrorCodes.NaoEncontrado,
                "Utilizador não encontrado.");
        }

        if (!utilizador.Ativo)
        {
            return OperationResult<UtilizadorResponse>.Ok(MapToResponse(utilizador));
        }

        utilizador.Ativo = false;

        await _auditService.LogAsync(
            AuditAcoes.UtilizadorDesativado,
            AuditResultados.Sucesso,
            actorId,
            ipAddress,
            targetEntidade: nameof(Utilizador),
            targetId: utilizador.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<UtilizadorResponse>.Ok(MapToResponse(utilizador));
    }

    private async Task<(string code, string message)?> ValidatePerfilLojaCombinationAsync(
        Perfil perfil,
        Guid? lojaId,
        CancellationToken cancellationToken)
    {
        var perfilNeedsLoja = perfil is Perfil.OperadorCaixa or Perfil.GerenteLoja;

        if (perfilNeedsLoja && lojaId is null)
        {
            return (UtilizadoresErrorCodes.LojaObrigatoria,
                "Operador e Gerente têm de estar associados a uma loja.");
        }

        if (!perfilNeedsLoja && lojaId is not null)
        {
            return (UtilizadoresErrorCodes.LojaNaoPermitida,
                "Administradores não podem ser associados a uma loja.");
        }

        if (lojaId is not null)
        {
            var loja = await _lojaRepository.GetByIdAsync(lojaId.Value, cancellationToken);
            if (loja is null)
            {
                return (UtilizadoresErrorCodes.LojaInexistente,
                    "A loja indicada não existe.");
            }
        }

        return null;
    }

    private static UtilizadorResponse MapToResponse(Utilizador u) => new()
    {
        Id = u.Id,
        Nome = u.Nome,
        Email = u.Email,
        Perfil = u.Perfil.ToString(),
        LojaId = u.LojaId,
        Ativo = u.Ativo,
        PasswordTemporaria = u.PasswordTemporaria,
        DataCriacao = u.DataCriacao,
        UltimoLogin = u.UltimoLogin
    };
}
