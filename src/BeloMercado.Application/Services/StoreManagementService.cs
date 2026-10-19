using BeloMercado.Application.Common;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Dtos.Lojas;
using BeloMercado.Application.Interfaces.Audit;
using BeloMercado.Application.Interfaces.Persistence;
using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Application.Interfaces.Time;
using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Services;

public class StoreManagementService
{
    private readonly ILojaRepository _lojaRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InventoryService _inventoryService;
    private readonly IClock _clock;

    public StoreManagementService(
        ILojaRepository lojaRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        InventoryService inventoryService,
        IClock clock)
    {
        _lojaRepository = lojaRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _inventoryService = inventoryService;
        _clock = clock;
    }

    public async Task<IReadOnlyList<LojaResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var lojas = await _lojaRepository.GetAllAsync(cancellationToken);
        return lojas.Select(MapToResponse).ToList();
    }

    public async Task<LojaResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var loja = await _lojaRepository.GetByIdAsync(id, cancellationToken);
        return loja is null ? null : MapToResponse(loja);
    }

    public async Task<OperationResult<LojaResponse>> CreateAsync(
        CreateLojaRequest request,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (request.StockMaxPorDefeito < request.StockMinPorDefeito)
        {
            return OperationResult<LojaResponse>.Validation(
                LojasErrorCodes.StockBoundsInvalidos,
                "Stock máximo tem de ser maior ou igual ao stock mínimo.");
        }

        if (await _lojaRepository.DesignacaoExistsAsync(request.Designacao, excludeId: null, cancellationToken))
        {
            return OperationResult<LojaResponse>.Conflict(
                LojasErrorCodes.DesignacaoDuplicada,
                "Já existe uma loja com essa designação.");
        }

        var loja = new Loja
        {
            Id = Guid.NewGuid(),
            Designacao = request.Designacao,
            Morada = request.Morada,
            CodigoPostal = request.CodigoPostal,
            Contacto = request.Contacto,
            Email = request.Email,
            Horario = request.Horario,
            StockMinPorDefeito = request.StockMinPorDefeito,
            StockMaxPorDefeito = request.StockMaxPorDefeito,
            Ativo = true,
            DataCriacao = _clock.UtcNow
        };

        await _lojaRepository.AddAsync(loja, cancellationToken);
        await _inventoryService.InitializeStockForNewLojaAsync(loja, cancellationToken);
        await _auditService.LogAsync(
            AuditAcoes.LojaCriada,
            AuditResultados.Sucesso,
            actorId,
            ipAddress,
            targetEntidade: nameof(Loja),
            targetId: loja.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<LojaResponse>.Ok(MapToResponse(loja));
    }

    public async Task<OperationResult<LojaResponse>> UpdateAsync(
        Guid id,
        UpdateLojaRequest request,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var loja = await _lojaRepository.GetByIdAsync(id, cancellationToken);
        if (loja is null)
        {
            return OperationResult<LojaResponse>.NotFound(
                LojasErrorCodes.NaoEncontrada,
                "Loja não encontrada.");
        }

        if (request.StockMaxPorDefeito < request.StockMinPorDefeito)
        {
            return OperationResult<LojaResponse>.Validation(
                LojasErrorCodes.StockBoundsInvalidos,
                "Stock máximo tem de ser maior ou igual ao stock mínimo.");
        }

        if (!string.Equals(loja.Designacao, request.Designacao, StringComparison.Ordinal)
            && await _lojaRepository.DesignacaoExistsAsync(request.Designacao, excludeId: id, cancellationToken))
        {
            return OperationResult<LojaResponse>.Conflict(
                LojasErrorCodes.DesignacaoDuplicada,
                "Já existe uma loja com essa designação.");
        }

        var ativoAnterior = loja.Ativo;

        loja.Designacao = request.Designacao;
        loja.Morada = request.Morada;
        loja.CodigoPostal = request.CodigoPostal;
        loja.Contacto = request.Contacto;
        loja.Email = request.Email;
        loja.Horario = request.Horario;
        loja.StockMinPorDefeito = request.StockMinPorDefeito;
        loja.StockMaxPorDefeito = request.StockMaxPorDefeito;
        loja.Ativo = request.Ativo;

        var acao = (ativoAnterior, request.Ativo) switch
        {
            (true, false) => AuditAcoes.LojaDesativada,
            (false, true) => AuditAcoes.LojaReativada,
            _ => AuditAcoes.LojaAtualizada
        };

        await _auditService.LogAsync(
            acao,
            AuditResultados.Sucesso,
            actorId,
            ipAddress,
            targetEntidade: nameof(Loja),
            targetId: loja.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<LojaResponse>.Ok(MapToResponse(loja));
    }

    private static LojaResponse MapToResponse(Loja loja) => new()
    {
        Id = loja.Id,
        Designacao = loja.Designacao,
        Morada = loja.Morada,
        CodigoPostal = loja.CodigoPostal,
        Contacto = loja.Contacto,
        Email = loja.Email,
        Horario = loja.Horario,
        StockMinPorDefeito = loja.StockMinPorDefeito,
        StockMaxPorDefeito = loja.StockMaxPorDefeito,
        Ativo = loja.Ativo,
        DataCriacao = loja.DataCriacao
    };
}
