using BeloMercado.Domain.Enums;

namespace BeloMercado.Domain.Entities;

public class Promocao
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid ProdutoId { get; set; }

    /// <summary>Loja específica onde a promoção é aplicada. NULL = todas as lojas.</summary>
    public Guid? LojaId { get; set; }

    public TipoPromocao Tipo { get; set; }

    /// <summary>
    /// Significado depende de Tipo:
    /// - Percentagem: percentagem de desconto (0..100)
    /// - PrecoFixo: novo preço unitário a aplicar
    /// </summary>
    public decimal Valor { get; set; }

    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public Produto? Produto { get; set; }
    public Loja? Loja { get; set; }
}
