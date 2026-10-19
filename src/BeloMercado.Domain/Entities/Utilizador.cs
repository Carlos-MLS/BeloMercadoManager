using BeloMercado.Domain.Enums;

namespace BeloMercado.Domain.Entities;

public class Utilizador
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool PasswordTemporaria { get; set; } = true;
    public Perfil Perfil { get; set; }
    public Guid? LojaId { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoLogin { get; set; }
    public int TentativasFalhadas { get; set; }
    public DateTime? BloqueadoAte { get; set; }
}
