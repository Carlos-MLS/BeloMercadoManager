namespace BeloMercado.Application.Dtos.Utilizadores;

public class UtilizadorResponse
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public Guid? LojaId { get; set; }
    public bool Ativo { get; set; }
    public bool PasswordTemporaria { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? UltimoLogin { get; set; }
}
