using BeloMercado.Application.Common;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Common.Validation;
using BeloMercado.Application.Dtos.Encomendas;
using BeloMercado.Application.Dtos.Fornecedores;
using BeloMercado.Application.Interfaces.Audit;
using BeloMercado.Application.Interfaces.Persistence;
using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Application.Interfaces.Time;
using BeloMercado.Domain.Entities;
using BeloMercado.Domain.Enums;

namespace BeloMercado.Application.Services;

/// <summary>
/// Owns UC09 (fornecedores), UC10 (encomendas) and UC11 (receção).
/// </summary>
public class ProcurementService
{
    /// <summary>UC10 RN02: encomendas com valor superior exigem aprovação Admin.</summary>
    private const decimal ApprovalThresholdEur = 5000m;

    private readonly IFornecedorRepository _fornecedorRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IEncomendaRepository _encomendaRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ProcurementService(
        IFornecedorRepository fornecedorRepository,
        IProdutoRepository produtoRepository,
        IEncomendaRepository encomendaRepository,
        IStockRepository stockRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _fornecedorRepository = fornecedorRepository;
        _produtoRepository = produtoRepository;
        _encomendaRepository = encomendaRepository;
        _stockRepository = stockRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<IReadOnlyList<FornecedorResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var fornecedores = await _fornecedorRepository.GetAllAsync(cancellationToken);
        return fornecedores.Select(MapToResponse).ToList();
    }

    public async Task<FornecedorResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var f = await _fornecedorRepository.GetByIdAsync(id, cancellationToken);
        return f is null ? null : MapToResponse(f);
    }

    public async Task<OperationResult<FornecedorResponse>> CreateAsync(
        CreateFornecedorRequest request,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!NifValidator.IsValid(request.Nif))
        {
            return OperationResult<FornecedorResponse>.Validation(
                FornecedoresErrorCodes.NifInvalido,
                "NIF inválido.");
        }

        if (await _fornecedorRepository.NifExistsAsync(request.Nif, excludeId: null, cancellationToken))
        {
            return OperationResult<FornecedorResponse>.Conflict(
                FornecedoresErrorCodes.NifDuplicado,
                "Já existe um fornecedor com esse NIF.");
        }

        var assocCheck = await ValidateAssociacoesAsync(request.ProdutosAssociados, cancellationToken);
        if (assocCheck is not null)
        {
            return OperationResult<FornecedorResponse>.Validation(assocCheck.Value.code, assocCheck.Value.message);
        }

        var fornecedor = new Fornecedor
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Nif = request.Nif,
            Morada = request.Morada,
            Contacto = request.Contacto,
            Email = request.Email,
            Ramo = request.Ramo,
            CondicoesPagamento = request.CondicoesPagamento,
            Ativo = true,
            DataCriacao = _clock.UtcNow
        };

        foreach (var pa in request.ProdutosAssociados)
        {
            fornecedor.ProdutosAssociados.Add(new ProdutoFornecedor
            {
                FornecedorId = fornecedor.Id,
                ProdutoId = pa.ProdutoId,
                PrecoCusto = pa.PrecoCusto,
                DescontoPercentagem = pa.DescontoPercentagem,
                PrazoEntregaDias = pa.PrazoEntregaDias
            });
        }

        await _fornecedorRepository.AddAsync(fornecedor, cancellationToken);
        await _auditService.LogAsync(
            AuditAcoes.FornecedorCriado, AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(Fornecedor), targetId: fornecedor.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with produto navigations populated for the response
        var loaded = await _fornecedorRepository.GetByIdAsync(fornecedor.Id, cancellationToken);
        return OperationResult<FornecedorResponse>.Ok(MapToResponse(loaded ?? fornecedor));
    }

    public async Task<OperationResult<FornecedorResponse>> UpdateAsync(
        Guid id,
        UpdateFornecedorRequest request,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var fornecedor = await _fornecedorRepository.GetByIdAsync(id, cancellationToken);
        if (fornecedor is null)
        {
            return OperationResult<FornecedorResponse>.NotFound(
                FornecedoresErrorCodes.NaoEncontrado,
                "Fornecedor não encontrado.");
        }

        var assocCheck = await ValidateAssociacoesAsync(request.ProdutosAssociados, cancellationToken);
        if (assocCheck is not null)
        {
            return OperationResult<FornecedorResponse>.Validation(assocCheck.Value.code, assocCheck.Value.message);
        }

        var ativoAnterior = fornecedor.Ativo;

        // NIF intencionalmente não alterado (UC09 alt 3a)
        fornecedor.Nome = request.Nome;
        fornecedor.Morada = request.Morada;
        fornecedor.Contacto = request.Contacto;
        fornecedor.Email = request.Email;
        fornecedor.Ramo = request.Ramo;
        fornecedor.CondicoesPagamento = request.CondicoesPagamento;
        fornecedor.Ativo = request.Ativo;

        // Replace product associations entirely
        await _fornecedorRepository.RemoveAssociacoesAsync(fornecedor.Id, cancellationToken);
        foreach (var pa in request.ProdutosAssociados)
        {
            fornecedor.ProdutosAssociados.Add(new ProdutoFornecedor
            {
                FornecedorId = fornecedor.Id,
                ProdutoId = pa.ProdutoId,
                PrecoCusto = pa.PrecoCusto,
                DescontoPercentagem = pa.DescontoPercentagem,
                PrazoEntregaDias = pa.PrazoEntregaDias
            });
        }

        var acao = (ativoAnterior, request.Ativo) switch
        {
            (true, false) => AuditAcoes.FornecedorDesativado,
            (false, true) => AuditAcoes.FornecedorReativado,
            _ => AuditAcoes.FornecedorAtualizado
        };
        await _auditService.LogAsync(
            acao, AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(Fornecedor), targetId: fornecedor.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var loaded = await _fornecedorRepository.GetByIdAsync(fornecedor.Id, cancellationToken);
        return OperationResult<FornecedorResponse>.Ok(MapToResponse(loaded ?? fornecedor));
    }

    private async Task<(string code, string message)?> ValidateAssociacoesAsync(
        List<ProdutoFornecedorRequest> associacoes,
        CancellationToken cancellationToken)
    {
        var seen = new HashSet<Guid>();
        foreach (var a in associacoes)
        {
            if (!seen.Add(a.ProdutoId))
            {
                return (FornecedoresErrorCodes.ProdutoDuplicadoNaAssociacao,
                    "Mesmo produto associado mais que uma vez.");
            }
            var p = await _produtoRepository.GetByIdAsync(a.ProdutoId, cancellationToken);
            if (p is null)
            {
                return (FornecedoresErrorCodes.ProdutoInexistente,
                    "Produto associado não existe.");
            }
            if (a.PrecoCusto < 0 || a.DescontoPercentagem < 0 || a.DescontoPercentagem > 100 || a.PrazoEntregaDias < 0)
            {
                return (FornecedoresErrorCodes.ValoresInvalidos,
                    "Valores numéricos da associação inválidos.");
            }
        }
        return null;
    }

    private static FornecedorResponse MapToResponse(Fornecedor f) => new()
    {
        Id = f.Id,
        Nome = f.Nome,
        Nif = f.Nif,
        Morada = f.Morada,
        Contacto = f.Contacto,
        Email = f.Email,
        Ramo = f.Ramo,
        CondicoesPagamento = f.CondicoesPagamento,
        Ativo = f.Ativo,
        DataCriacao = f.DataCriacao,
        ProdutosAssociados = f.ProdutosAssociados.Select(pa => new ProdutoFornecedorResponse
        {
            ProdutoId = pa.ProdutoId,
            ProdutoEan13 = pa.Produto?.Ean13 ?? string.Empty,
            ProdutoDescricao = pa.Produto?.Descricao ?? string.Empty,
            PrecoCusto = pa.PrecoCusto,
            DescontoPercentagem = pa.DescontoPercentagem,
            PrazoEntregaDias = pa.PrazoEntregaDias
        }).ToList()
    };

    // ---------- UC10 ----------

    public async Task<IReadOnlyList<EncomendaResponse>> ListEncomendasAsync(
        Guid? scopeLojaId,
        CancellationToken cancellationToken = default)
    {
        var encomendas = scopeLojaId.HasValue
            ? await _encomendaRepository.ListByLojaAsync(scopeLojaId.Value, cancellationToken)
            : await _encomendaRepository.ListAllAsync(cancellationToken);
        return encomendas.Select(MapEncomenda).ToList();
    }

    public async Task<EncomendaResponse?> GetEncomendaByIdAsync(
        Guid id,
        Guid? scopeLojaId,
        CancellationToken cancellationToken = default)
    {
        var enc = await _encomendaRepository.GetByIdAsync(id, cancellationToken);
        if (enc is null
            || (scopeLojaId.HasValue && enc.LojaId != scopeLojaId.Value))
        {
            return null;
        }
        return MapEncomenda(enc);
    }

    public async Task<OperationResult<EncomendaResponse>> CriarEncomendaAsync(
        CriarEncomendaRequest request,
        Guid actorId,
        Guid? actorLojaId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        // Gerente é forçado à sua loja; Admin (sem loja) pode escolher via request.LojaId.
        var lojaDestinoId = actorLojaId ?? request.LojaId;
        if (lojaDestinoId is null)
        {
            return OperationResult<EncomendaResponse>.Validation(
                EncomendasErrorCodes.LojaObrigatoria,
                "Tens de indicar uma loja destinatária para a encomenda.");
        }
        actorLojaId = lojaDestinoId;

        if (request.Linhas.Count == 0)
        {
            return OperationResult<EncomendaResponse>.Validation(
                EncomendasErrorCodes.SemLinhas,
                "A encomenda tem de ter pelo menos uma linha.");
        }

        var fornecedor = await _fornecedorRepository.GetByIdAsync(request.FornecedorId, cancellationToken);
        if (fornecedor is null)
        {
            return OperationResult<EncomendaResponse>.Validation(
                EncomendasErrorCodes.FornecedorNaoEncontrado,
                "Fornecedor não encontrado.");
        }
        if (!fornecedor.Ativo)
        {
            return OperationResult<EncomendaResponse>.Validation(
                EncomendasErrorCodes.FornecedorInativo,
                "Fornecedor está inativo.");
        }

        // Build linhas with snapshots from ProdutoFornecedor association
        var assocByProd = fornecedor.ProdutosAssociados.ToDictionary(pa => pa.ProdutoId);
        var linhas = new List<LinhaEncomenda>(request.Linhas.Count);
        decimal total = 0m;
        foreach (var l in request.Linhas)
        {
            if (l.Quantidade <= 0)
            {
                return OperationResult<EncomendaResponse>.Validation(
                    EncomendasErrorCodes.QuantidadeInvalida,
                    "Quantidade tem de ser positiva.");
            }
            if (!assocByProd.TryGetValue(l.ProdutoId, out var assoc))
            {
                return OperationResult<EncomendaResponse>.Validation(
                    EncomendasErrorCodes.ProdutoSemAssociacao,
                    $"Produto {l.ProdutoId} não está associado a este fornecedor.");
            }
            var produto = await _produtoRepository.GetByIdAsync(l.ProdutoId, cancellationToken);
            // produto must exist (otherwise the association wouldn't be valid)
            // — defensive check avoids null deref later
            if (produto is null)
            {
                return OperationResult<EncomendaResponse>.Validation(
                    EncomendasErrorCodes.ProdutoSemAssociacao,
                    $"Produto {l.ProdutoId} não encontrado.");
            }

            var subtotalSemDesconto = assoc.PrecoCusto * l.Quantidade;
            var subtotal = Math.Round(
                subtotalSemDesconto * (1 - assoc.DescontoPercentagem / 100m),
                2, MidpointRounding.AwayFromZero);

            linhas.Add(new LinhaEncomenda
            {
                Id = Guid.NewGuid(),
                ProdutoId = produto.Id,
                ProdutoDescricao = produto.Descricao,
                ProdutoEan13 = produto.Ean13,
                Quantidade = l.Quantidade,
                PrecoCustoUnitario = assoc.PrecoCusto,
                DescontoPercentagem = assoc.DescontoPercentagem,
                Subtotal = subtotal,
                QuantidadeRecebida = 0
            });
            total += subtotal;
        }

        var encomenda = new Encomenda
        {
            Id = Guid.NewGuid(),
            FornecedorId = fornecedor.Id,
            LojaId = actorLojaId.Value,
            CriadoPorId = actorId,
            Estado = EstadoEncomenda.Rascunho,
            DataCriacao = _clock.UtcNow,
            Total = Math.Round(total, 2)
        };
        foreach (var l in linhas)
        {
            l.EncomendaId = encomenda.Id;
            encomenda.Linhas.Add(l);
        }
        await _encomendaRepository.AddAsync(encomenda, cancellationToken);

        await _auditService.LogAsync(
            AuditAcoes.EncomendaCriada, AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(Encomenda), targetId: encomenda.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload to populate Fornecedor navigation for the response
        var loaded = await _encomendaRepository.GetByIdAsync(encomenda.Id, cancellationToken);
        return OperationResult<EncomendaResponse>.Ok(MapEncomenda(loaded ?? encomenda));
    }

    public async Task<OperationResult<EncomendaResponse>> SubmeterEncomendaAsync(
        Guid id,
        Guid actorId,
        Guid? actorLojaId,
        bool isAdmin,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var enc = await _encomendaRepository.GetByIdAsync(id, cancellationToken);
        if (enc is null
            || (actorLojaId.HasValue && !isAdmin && enc.LojaId != actorLojaId.Value))
        {
            return OperationResult<EncomendaResponse>.NotFound(
                EncomendasErrorCodes.NaoEncontrada,
                "Encomenda não encontrada.");
        }

        if (enc.Estado != EstadoEncomenda.Rascunho)
        {
            return OperationResult<EncomendaResponse>.Conflict(
                EncomendasErrorCodes.EstadoInvalido,
                "Apenas encomendas em rascunho podem ser submetidas.");
        }

        // RN02: > 5000 € requer aprovação do Admin (UC10 alt 8a)
        if (enc.Total > ApprovalThresholdEur && !isAdmin)
        {
            return OperationResult<EncomendaResponse>.Forbidden(
                EncomendasErrorCodes.ApprovalRequired,
                "Encomendas superiores a 5000€ requerem aprovação do Administrador.");
        }

        enc.Estado = EstadoEncomenda.Submetida;
        enc.DataSubmissao = _clock.UtcNow;

        await _auditService.LogAsync(
            AuditAcoes.EncomendaSubmetida, AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(Encomenda), targetId: enc.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<EncomendaResponse>.Ok(MapEncomenda(enc));
    }

    // ---------- UC11 ----------

    public async Task<OperationResult<EncomendaResponse>> RececionarEncomendaAsync(
        Guid id,
        RececionarEncomendaRequest request,
        Guid actorId,
        Guid? actorLojaId,
        bool isAdmin,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var enc = await _encomendaRepository.GetByIdAsync(id, cancellationToken);
        if (enc is null
            || (actorLojaId.HasValue && !isAdmin && enc.LojaId != actorLojaId.Value))
        {
            return OperationResult<EncomendaResponse>.NotFound(
                EncomendasErrorCodes.NaoEncontrada,
                "Encomenda não encontrada.");
        }

        if (enc.Estado != EstadoEncomenda.Submetida
            && enc.Estado != EstadoEncomenda.ParcialmenteRecebida)
        {
            return OperationResult<EncomendaResponse>.Conflict(
                EncomendasErrorCodes.EstadoNaoRececionavel,
                "Apenas encomendas Submetidas ou Parcialmente Recebidas podem ser rececionadas.");
        }

        if (request.Linhas.Count == 0)
        {
            return OperationResult<EncomendaResponse>.Validation(
                EncomendasErrorCodes.SemLinhas,
                "Pelo menos uma linha tem de ser indicada.");
        }

        var linhasById = enc.Linhas.ToDictionary(l => l.Id);
        // First pass: validate all linhas (no DB writes yet)
        foreach (var lr in request.Linhas)
        {
            if (!linhasById.TryGetValue(lr.LinhaId, out var encLinha))
            {
                return OperationResult<EncomendaResponse>.Validation(
                    EncomendasErrorCodes.LinhaNaoEncontrada,
                    $"Linha {lr.LinhaId} não pertence a esta encomenda.");
            }
            if (lr.QuantidadeRecebida < 0)
            {
                return OperationResult<EncomendaResponse>.Validation(
                    EncomendasErrorCodes.QuantidadeRecebidaInvalida,
                    "Quantidade recebida não pode ser negativa.");
            }
            if (encLinha.QuantidadeRecebida + lr.QuantidadeRecebida > encLinha.Quantidade)
            {
                return OperationResult<EncomendaResponse>.Validation(
                    EncomendasErrorCodes.QuantidadeRecebidaInvalida,
                    $"Quantidade recebida total para linha {lr.LinhaId} excede a pedida ({encLinha.Quantidade}).");
            }
        }

        // Second pass: increment Stock and update LinhaEncomenda
        foreach (var lr in request.Linhas)
        {
            if (lr.QuantidadeRecebida == 0)
            {
                continue;
            }
            var encLinha = linhasById[lr.LinhaId];
            var stock = await _stockRepository.GetByLojaProdutoAsync(
                enc.LojaId, encLinha.ProdutoId, cancellationToken);
            if (stock is null)
            {
                return OperationResult<EncomendaResponse>.Validation(
                    EncomendasErrorCodes.StockNaoMaterializado,
                    $"Stock não materializado para o produto {encLinha.ProdutoEan13} na loja.");
            }
            stock.Quantidade += lr.QuantidadeRecebida;
            encLinha.QuantidadeRecebida += lr.QuantidadeRecebida;
        }

        var allFull = enc.Linhas.All(l => l.QuantidadeRecebida >= l.Quantidade);
        var now = _clock.UtcNow;
        if (allFull)
        {
            enc.Estado = EstadoEncomenda.Recebida;
            enc.DataRececao = now;
        }
        else
        {
            enc.Estado = EstadoEncomenda.ParcialmenteRecebida;
        }

        await _auditService.LogAsync(
            allFull ? AuditAcoes.EncomendaRecebida : AuditAcoes.EncomendaParcialmenteRecebida,
            AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(Encomenda), targetId: enc.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<EncomendaResponse>.Ok(MapEncomenda(enc));
    }

    /// <summary>
    /// UC10 — Edita uma encomenda em Rascunho. Substitui as linhas existentes
    /// pelas indicadas no request. O fornecedor pode ser alterado se ainda em
    /// rascunho. Total é recalculado.
    /// </summary>
    public async Task<OperationResult<EncomendaResponse>> AtualizarEncomendaRascunhoAsync(
        Guid id,
        CriarEncomendaRequest request,
        Guid actorId,
        Guid? actorLojaId,
        bool isAdmin,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var enc = await _encomendaRepository.GetByIdAsync(id, cancellationToken);
        if (enc is null
            || (actorLojaId.HasValue && !isAdmin && enc.LojaId != actorLojaId.Value))
        {
            return OperationResult<EncomendaResponse>.NotFound(
                EncomendasErrorCodes.NaoEncontrada,
                "Encomenda não encontrada.");
        }

        if (enc.Estado != EstadoEncomenda.Rascunho)
        {
            return OperationResult<EncomendaResponse>.Conflict(
                EncomendasErrorCodes.EstadoInvalido,
                "Apenas encomendas em rascunho podem ser editadas.");
        }

        if (request.Linhas.Count == 0)
        {
            return OperationResult<EncomendaResponse>.Validation(
                EncomendasErrorCodes.SemLinhas,
                "A encomenda tem de ter pelo menos uma linha.");
        }

        var fornecedor = await _fornecedorRepository.GetByIdAsync(request.FornecedorId, cancellationToken);
        if (fornecedor is null || !fornecedor.Ativo)
        {
            return OperationResult<EncomendaResponse>.Validation(
                EncomendasErrorCodes.FornecedorNaoEncontrado,
                "Fornecedor inválido.");
        }

        var assocByProd = fornecedor.ProdutosAssociados.ToDictionary(pa => pa.ProdutoId);
        var novasLinhas = new List<LinhaEncomenda>();
        decimal total = 0m;
        foreach (var l in request.Linhas)
        {
            if (l.Quantidade <= 0)
            {
                return OperationResult<EncomendaResponse>.Validation(
                    EncomendasErrorCodes.QuantidadeInvalida,
                    "Quantidade tem de ser positiva.");
            }
            if (!assocByProd.TryGetValue(l.ProdutoId, out var assoc))
            {
                return OperationResult<EncomendaResponse>.Validation(
                    EncomendasErrorCodes.ProdutoSemAssociacao,
                    $"Produto {l.ProdutoId} não está associado a este fornecedor.");
            }
            var produto = await _produtoRepository.GetByIdAsync(l.ProdutoId, cancellationToken);
            if (produto is null)
            {
                return OperationResult<EncomendaResponse>.Validation(
                    EncomendasErrorCodes.ProdutoSemAssociacao,
                    $"Produto {l.ProdutoId} não encontrado.");
            }
            var subtotalSemDesc = assoc.PrecoCusto * l.Quantidade;
            var subtotal = Math.Round(
                subtotalSemDesc * (1 - assoc.DescontoPercentagem / 100m),
                2, MidpointRounding.AwayFromZero);
            novasLinhas.Add(new LinhaEncomenda
            {
                Id = Guid.NewGuid(),
                EncomendaId = enc.Id,
                ProdutoId = produto.Id,
                ProdutoDescricao = produto.Descricao,
                ProdutoEan13 = produto.Ean13,
                Quantidade = l.Quantidade,
                PrecoCustoUnitario = assoc.PrecoCusto,
                DescontoPercentagem = assoc.DescontoPercentagem,
                Subtotal = subtotal,
                QuantidadeRecebida = 0
            });
            total += subtotal;
        }

        // Substituição em 2 passos: primeiro apaga as antigas e persiste,
        // depois adiciona as novas via DbSet.AddRange (estado Added explícito).
        // Não adicionamos pelo navigation enc.Linhas porque o EF, perante
        // entidades com Guid já preenchido, hesita entre INSERT e UPDATE.
        var linhasAntigas = enc.Linhas.ToList();
        _encomendaRepository.RemoveLinhas(linhasAntigas);
        enc.Linhas.Clear();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var nl in novasLinhas)
        {
            nl.EncomendaId = enc.Id;
        }
        await _encomendaRepository.AddLinhasAsync(novasLinhas, cancellationToken);
        enc.FornecedorId = fornecedor.Id;
        enc.Total = Math.Round(total, 2);

        await _auditService.LogAsync(
            AuditAcoes.EncomendaCriada, AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(Encomenda), targetId: enc.Id,
            detalhes: "Rascunho atualizado",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var loaded = await _encomendaRepository.GetByIdAsync(enc.Id, cancellationToken);
        return OperationResult<EncomendaResponse>.Ok(MapEncomenda(loaded ?? enc));
    }

    /// <summary>
    /// UC10 — Cancela (apaga) uma encomenda em Rascunho. Outras transições
    /// (Submetida → Cancelada) ainda não são suportadas.
    /// </summary>
    public async Task<OperationResult<EncomendaResponse>> CancelarEncomendaAsync(
        Guid id,
        Guid actorId,
        Guid? actorLojaId,
        bool isAdmin,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var enc = await _encomendaRepository.GetByIdAsync(id, cancellationToken);
        if (enc is null
            || (actorLojaId.HasValue && !isAdmin && enc.LojaId != actorLojaId.Value))
        {
            return OperationResult<EncomendaResponse>.NotFound(
                EncomendasErrorCodes.NaoEncontrada,
                "Encomenda não encontrada.");
        }
        if (enc.Estado != EstadoEncomenda.Rascunho)
        {
            return OperationResult<EncomendaResponse>.Conflict(
                EncomendasErrorCodes.EstadoInvalido,
                "Só é possível cancelar encomendas em rascunho.");
        }

        _encomendaRepository.RemoveLinhas(enc.Linhas.ToList());
        _encomendaRepository.Remove(enc);

        await _auditService.LogAsync(
            AuditAcoes.EncomendaCriada, AuditResultados.Sucesso, actorId, ipAddress,
            targetEntidade: nameof(Encomenda), targetId: enc.Id,
            detalhes: "Rascunho cancelado",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<EncomendaResponse>.Ok(MapEncomenda(enc));
    }

    private static EncomendaResponse MapEncomenda(Encomenda e) => new()
    {
        Id = e.Id,
        FornecedorId = e.FornecedorId,
        FornecedorNome = e.Fornecedor?.Nome ?? string.Empty,
        LojaId = e.LojaId,
        CriadoPorId = e.CriadoPorId,
        Estado = e.Estado.ToString(),
        DataCriacao = e.DataCriacao,
        DataSubmissao = e.DataSubmissao,
        DataRececao = e.DataRececao,
        Total = e.Total,
        Linhas = e.Linhas.Select(l => new LinhaEncomendaResponse
        {
            Id = l.Id,
            ProdutoId = l.ProdutoId,
            ProdutoEan13 = l.ProdutoEan13,
            ProdutoDescricao = l.ProdutoDescricao,
            Quantidade = l.Quantidade,
            PrecoCustoUnitario = l.PrecoCustoUnitario,
            DescontoPercentagem = l.DescontoPercentagem,
            Subtotal = l.Subtotal,
            QuantidadeRecebida = l.QuantidadeRecebida
        }).ToList()
    };
}
