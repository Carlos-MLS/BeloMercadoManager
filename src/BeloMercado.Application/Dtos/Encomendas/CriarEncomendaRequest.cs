using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Encomendas;

public class CriarEncomendaRequest
{
    [Required]
    public Guid FornecedorId { get; set; }

    /// <summary>
    /// Loja destinatária. Para Gerente, é ignorada (forçada à loja do JWT).
    /// Para AdminCadeia (que não tem loja), é obrigatória.
    /// </summary>
    public Guid? LojaId { get; set; }

    [Required, MinLength(1)]
    public List<LinhaEncomendaRequest> Linhas { get; set; } = new();
}
