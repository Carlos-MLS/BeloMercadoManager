using System.ComponentModel.DataAnnotations;

namespace BeloMercado.Application.Dtos.Catalogo;

public class CreateCategoriaRequest
{
    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public Guid? ParentCategoriaId { get; set; }
}
