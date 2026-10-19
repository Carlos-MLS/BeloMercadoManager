namespace BeloMercado.Application.Dtos.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Perfil { get; set; } = string.Empty;
    public Guid? LojaId { get; set; }
}
