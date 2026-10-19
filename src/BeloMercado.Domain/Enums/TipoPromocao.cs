namespace BeloMercado.Domain.Enums;

public enum TipoPromocao
{
    /// <summary>Valor representa percentagem de desconto (0..100).</summary>
    Percentagem = 1,

    /// <summary>Valor representa novo preço unitário fixo.</summary>
    PrecoFixo = 2
}
