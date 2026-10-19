using BeloMercado.Application.Common;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Common.Validation;
using BeloMercado.Application.Dtos.Catalogo;
using BeloMercado.Application.Interfaces.Audit;
using BeloMercado.Application.Interfaces.Persistence;
using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Application.Interfaces.Time;
using BeloMercado.Domain.Entities;

namespace BeloMercado.Application.Services;

public class CatalogService
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InventoryService _inventoryService;
    private readonly IClock _clock;

    public CatalogService(
        ICategoriaRepository categoriaRepository,
        IProdutoRepository produtoRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        InventoryService inventoryService,
        IClock clock)
    {
        _categoriaRepository = categoriaRepository;
        _produtoRepository = produtoRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _inventoryService = inventoryService;
        _clock = clock;
    }

    public async Task<IReadOnlyList<CategoriaResponse>> ListCategoriasAsync(CancellationToken cancellationToken = default)
    {
        var cats = await _categoriaRepository.GetAllAsync(cancellationToken);
        return cats.Select(MapCategoria).ToList();
    }

    public async Task<OperationResult<CategoriaResponse>> CreateCategoriaAsync(
        CreateCategoriaRequest request,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (await _categoriaRepository.NomeExistsAsync(request.Nome, cancellationToken))
        {
            return OperationResult<CategoriaResponse>.Conflict(
                CatalogoErrorCodes.CategoriaNomeDuplicado,
                "Já existe uma categoria com esse nome.");
        }

        if (request.ParentCategoriaId is not null
            && await _categoriaRepository.GetByIdAsync(request.ParentCategoriaId.Value, cancellationToken) is null)
        {
            return OperationResult<CategoriaResponse>.Validation(
                CatalogoErrorCodes.CategoriaParentInexistente,
                "A categoria pai indicada não existe.");
        }

        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            ParentCategoriaId = request.ParentCategoriaId,
            Ativo = true,
            DataCriacao = _clock.UtcNow
        };

        await _categoriaRepository.AddAsync(categoria, cancellationToken);
        await _auditService.LogAsync(
            AuditAcoes.CategoriaCriada,
            AuditResultados.Sucesso,
            actorId,
            ipAddress,
            targetEntidade: nameof(Categoria),
            targetId: categoria.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<CategoriaResponse>.Ok(MapCategoria(categoria));
    }

    public async Task<IReadOnlyList<ProdutoResponse>> ListProdutosAsync(CancellationToken cancellationToken = default)
    {
        var produtos = await _produtoRepository.GetAllAsync(cancellationToken);
        return produtos.Select(MapProduto).ToList();
    }

    public async Task<ProdutoResponse?> GetProdutoByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var produto = await _produtoRepository.GetByIdAsync(id, cancellationToken);
        return produto is null ? null : MapProduto(produto);
    }

    public async Task<OperationResult<ProdutoResponse>> CreateProdutoAsync(
        CreateProdutoRequest request,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!Ean13Validator.IsValid(request.Ean13))
        {
            return OperationResult<ProdutoResponse>.Validation(
                CatalogoErrorCodes.Ean13Invalido,
                "EAN-13 inválido (deve ter 13 dígitos com check digit correto).");
        }

        if (await _categoriaRepository.GetByIdAsync(request.CategoriaId, cancellationToken) is null)
        {
            return OperationResult<ProdutoResponse>.Validation(
                CatalogoErrorCodes.CategoriaNaoEncontrada,
                "A categoria indicada não existe.");
        }

        if (await _produtoRepository.Ean13ExistsAsync(request.Ean13, excludeId: null, cancellationToken))
        {
            return OperationResult<ProdutoResponse>.Conflict(
                CatalogoErrorCodes.Ean13Duplicado,
                "Já existe um produto com esse EAN-13.");
        }

        var now = _clock.UtcNow;
        var produto = new Produto
        {
            Id = Guid.NewGuid(),
            Ean13 = request.Ean13,
            Descricao = request.Descricao,
            CategoriaId = request.CategoriaId,
            Preco = request.Preco,
            TaxaIva = request.TaxaIva,
            UnidadeMedida = request.UnidadeMedida,
            ImagemUrl = request.ImagemUrl,
            Ativo = true,
            DataCriacao = now
        };

        await _produtoRepository.AddAsync(produto, cancellationToken);
        await _produtoRepository.AddHistoricoPrecoAsync(new HistoricoPreco
        {
            Id = Guid.NewGuid(),
            ProdutoId = produto.Id,
            Preco = produto.Preco,
            DataMudanca = now,
            AlteradoPorUtilizadorId = actorId
        }, cancellationToken);
        await _inventoryService.InitializeStockForNewProdutoAsync(produto.Id, cancellationToken);

        await _auditService.LogAsync(
            AuditAcoes.ProdutoCriado,
            AuditResultados.Sucesso,
            actorId,
            ipAddress,
            targetEntidade: nameof(Produto),
            targetId: produto.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with categoria for response mapping (the ref might be null
        // in the fresh entity since we never queried the categoria back).
        produto.Categoria = await _categoriaRepository.GetByIdAsync(produto.CategoriaId, cancellationToken);
        return OperationResult<ProdutoResponse>.Ok(MapProduto(produto));
    }

    public async Task<OperationResult<ProdutoResponse>> UpdateProdutoAsync(
        Guid id,
        UpdateProdutoRequest request,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var produto = await _produtoRepository.GetByIdAsync(id, cancellationToken);
        if (produto is null)
        {
            return OperationResult<ProdutoResponse>.NotFound(
                CatalogoErrorCodes.ProdutoNaoEncontrado,
                "Produto não encontrado.");
        }

        if (await _categoriaRepository.GetByIdAsync(request.CategoriaId, cancellationToken) is null)
        {
            return OperationResult<ProdutoResponse>.Validation(
                CatalogoErrorCodes.CategoriaNaoEncontrada,
                "A categoria indicada não existe.");
        }

        var precoMudou = produto.Preco != request.Preco;
        var ativoAnterior = produto.Ativo;
        var now = _clock.UtcNow;

        produto.Descricao = request.Descricao;
        produto.CategoriaId = request.CategoriaId;
        produto.Preco = request.Preco;
        produto.TaxaIva = request.TaxaIva;
        produto.UnidadeMedida = request.UnidadeMedida;
        produto.ImagemUrl = request.ImagemUrl;
        produto.Ativo = request.Ativo;

        if (precoMudou)
        {
            await _produtoRepository.AddHistoricoPrecoAsync(new HistoricoPreco
            {
                Id = Guid.NewGuid(),
                ProdutoId = produto.Id,
                Preco = produto.Preco,
                DataMudanca = now,
                AlteradoPorUtilizadorId = actorId
            }, cancellationToken);
            await _auditService.LogAsync(
                AuditAcoes.ProdutoPrecoAlterado,
                AuditResultados.Sucesso,
                actorId,
                ipAddress,
                targetEntidade: nameof(Produto),
                targetId: produto.Id,
                cancellationToken: cancellationToken);
        }

        var acao = (ativoAnterior, request.Ativo) switch
        {
            (true, false) => AuditAcoes.ProdutoDesativado,
            (false, true) => AuditAcoes.ProdutoReativado,
            _ => AuditAcoes.ProdutoAtualizado
        };
        await _auditService.LogAsync(
            acao,
            AuditResultados.Sucesso,
            actorId,
            ipAddress,
            targetEntidade: nameof(Produto),
            targetId: produto.Id,
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        produto.Categoria = await _categoriaRepository.GetByIdAsync(produto.CategoriaId, cancellationToken);
        return OperationResult<ProdutoResponse>.Ok(MapProduto(produto));
    }

    public async Task<OperationResult<ProdutoResponse>> DeactivateProdutoAsync(
        Guid id,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var produto = await _produtoRepository.GetByIdAsync(id, cancellationToken);
        if (produto is null)
        {
            return OperationResult<ProdutoResponse>.NotFound(
                CatalogoErrorCodes.ProdutoNaoEncontrado,
                "Produto não encontrado.");
        }

        if (!produto.Ativo)
        {
            return OperationResult<ProdutoResponse>.Ok(MapProduto(produto));
        }

        produto.Ativo = false;

        await _auditService.LogAsync(
            AuditAcoes.ProdutoDesativado,
            AuditResultados.Sucesso,
            actorId,
            ipAddress,
            targetEntidade: nameof(Produto),
            targetId: produto.Id,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<ProdutoResponse>.Ok(MapProduto(produto));
    }

    private static CategoriaResponse MapCategoria(Categoria c) => new()
    {
        Id = c.Id,
        Nome = c.Nome,
        ParentCategoriaId = c.ParentCategoriaId,
        Ativo = c.Ativo,
        DataCriacao = c.DataCriacao
    };

    private static ProdutoResponse MapProduto(Produto p) => new()
    {
        Id = p.Id,
        Ean13 = p.Ean13,
        Descricao = p.Descricao,
        CategoriaId = p.CategoriaId,
        CategoriaNome = p.Categoria?.Nome ?? string.Empty,
        Preco = p.Preco,
        TaxaIva = p.TaxaIva.ToString(),
        TaxaIvaPercentagem = (int)p.TaxaIva,
        UnidadeMedida = p.UnidadeMedida,
        ImagemUrl = p.ImagemUrl,
        Ativo = p.Ativo,
        DataCriacao = p.DataCriacao
    };
}
