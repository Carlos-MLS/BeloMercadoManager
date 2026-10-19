using BeloMercado.Application.Common;
using BeloMercado.Application.Common.Constants;
using BeloMercado.Application.Common.Validation;
using BeloMercado.Application.Dtos.Auth;
using BeloMercado.Application.Dtos.Me;
using BeloMercado.Application.Interfaces.Audit;
using BeloMercado.Application.Interfaces.Auth;
using BeloMercado.Application.Interfaces.Persistence;
using BeloMercado.Application.Interfaces.Repositories;
using BeloMercado.Application.Settings;
using BeloMercado.Domain.Entities;
using Microsoft.Extensions.Options;

namespace BeloMercado.Application.Services;

public class AuthAccessService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILojaRepository _lojaRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthSettings _authSettings;
    private readonly JwtSettings _jwtSettings;

    public AuthAccessService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ILojaRepository lojaRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        IOptions<AuthSettings> authSettings,
        IOptions<JwtSettings> jwtSettings)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _lojaRepository = lojaRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _authSettings = authSettings.Value;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Always run BCrypt to keep response timing constant — mitigates user
        // enumeration via timing (CLAUDE.md UC15 timing-safe rule).
        var passwordValid = _passwordHasher.Verify(request.Password, user?.PasswordHash);

        var loginValid = user is not null
            && user.Ativo
            && (user.BloqueadoAte is null || user.BloqueadoAte <= now)
            && passwordValid;

        if (!loginValid)
        {
            await HandleFailedLoginAsync(user, now, ipAddress, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        // Login success
        user!.TentativasFalhadas = 0;
        user.BloqueadoAte = null;
        user.UltimoLogin = now;

        var accessToken = _tokenGenerator.GenerateAccessToken(user);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();
        var refreshExpiresAt = now.AddDays(_jwtSettings.RefreshTokenLifetimeDays);

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UtilizadorId = user.Id,
            TokenHash = refreshToken.TokenHash,
            ExpiresAt = refreshExpiresAt,
            CreatedAt = now
        };
        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        await _auditService.LogAsync(
            AuditAcoes.LoginSucesso,
            AuditResultados.Sucesso,
            user.Id,
            ipAddress,
            targetEntidade: nameof(Utilizador),
            targetId: user.Id,
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessExpiresAt = now.AddMinutes(_jwtSettings.AccessTokenLifetimeMinutes);
        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.RawToken,
            ExpiresAt = accessExpiresAt,
            Perfil = user.Perfil.ToString(),
            LojaId = user.LojaId
        };
    }

    public async Task<RefreshResponse?> RefreshAsync(
        RefreshRequest request,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var tokenHash = _tokenGenerator.HashRefreshToken(request.RefreshToken);
        var stored = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (stored is null || !stored.IsActive(now))
        {
            await _auditService.LogAsync(
                AuditAcoes.TokenRefreshFalha,
                AuditResultados.Falha,
                stored?.UtilizadorId,
                ipAddress,
                cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        var user = await _userRepository.GetByIdAsync(stored.UtilizadorId, cancellationToken);
        if (user is null || !user.Ativo)
        {
            stored.RevokedAt = now;
            await _auditService.LogAsync(
                AuditAcoes.TokenRefreshFalha,
                AuditResultados.Falha,
                stored.UtilizadorId,
                ipAddress,
                cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        // Rotate: revoke old token, issue new one
        var newRefreshToken = _tokenGenerator.GenerateRefreshToken();
        var newRefreshExpiresAt = now.AddDays(_jwtSettings.RefreshTokenLifetimeDays);
        var newRefreshEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UtilizadorId = user.Id,
            TokenHash = newRefreshToken.TokenHash,
            ExpiresAt = newRefreshExpiresAt,
            CreatedAt = now
        };
        await _refreshTokenRepository.AddAsync(newRefreshEntity, cancellationToken);

        stored.RevokedAt = now;
        stored.ReplacedByTokenId = newRefreshEntity.Id;

        var accessToken = _tokenGenerator.GenerateAccessToken(user);
        var accessExpiresAt = now.AddMinutes(_jwtSettings.AccessTokenLifetimeMinutes);

        await _auditService.LogAsync(
            AuditAcoes.TokenRefreshed,
            AuditResultados.Sucesso,
            user.Id,
            ipAddress,
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefreshResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.RawToken,
            ExpiresAt = accessExpiresAt
        };
    }

    public async Task LogoutAsync(
        string refreshToken,
        Guid? utilizadorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var tokenHash = _tokenGenerator.HashRefreshToken(refreshToken);
        var stored = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = now;
        }

        await _auditService.LogAsync(
            AuditAcoes.LogoutSucesso,
            AuditResultados.Sucesso,
            utilizadorId,
            ipAddress,
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleFailedLoginAsync(
        Utilizador? user,
        DateTime now,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        // Don't penalise unknown emails or already-locked accounts.
        if (user is not null
            && user.Ativo
            && (user.BloqueadoAte is null || user.BloqueadoAte <= now))
        {
            user.TentativasFalhadas++;
            if (user.TentativasFalhadas >= _authSettings.MaxFailedAttempts)
            {
                user.BloqueadoAte = now.AddMinutes(_authSettings.LockoutMinutes);
                user.TentativasFalhadas = 0;
                await _auditService.LogAsync(
                    AuditAcoes.ContaBloqueada,
                    AuditResultados.Sucesso,
                    user.Id,
                    ipAddress,
                    targetEntidade: nameof(Utilizador),
                    targetId: user.Id,
                    cancellationToken: cancellationToken);
            }
        }

        await _auditService.LogAsync(
            AuditAcoes.LoginFalha,
            AuditResultados.Falha,
            user?.Id,
            ipAddress,
            cancellationToken: cancellationToken);
    }

    // ---------- UC17 ----------

    public async Task<MeResponse?> GetMeAsync(Guid utilizadorId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(utilizadorId, cancellationToken);
        if (user is null) return null;
        var lojaDesignacao = await ResolveLojaDesignacaoAsync(user.LojaId, cancellationToken);
        return MapMe(user, lojaDesignacao);
    }

    public async Task<OperationResult<MeResponse>> UpdateMeAsync(
        Guid utilizadorId,
        UpdateMeRequest request,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(utilizadorId, cancellationToken);
        if (user is null)
        {
            return OperationResult<MeResponse>.NotFound(
                PerfilErrorCodes.UtilizadorNaoEncontrado,
                "Utilizador não encontrado.");
        }

        // RN01: email/perfil/lojaId NOT editable here. Only Nome.
        user.Nome = request.Nome;

        await _auditService.LogAsync(
            AuditAcoes.PerfilAtualizado, AuditResultados.Sucesso, utilizadorId, ipAddress,
            targetEntidade: nameof(Utilizador), targetId: utilizadorId,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var lojaDesignacao = await ResolveLojaDesignacaoAsync(user.LojaId, cancellationToken);

        return OperationResult<MeResponse>.Ok(MapMe(user, lojaDesignacao));
    }

    public async Task<OperationResult<MeResponse>> ChangePasswordAsync(
        Guid utilizadorId,
        ChangePasswordRequest request,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(utilizadorId, cancellationToken);
        if (user is null)
        {
            return OperationResult<MeResponse>.NotFound(
                PerfilErrorCodes.UtilizadorNaoEncontrado,
                "Utilizador não encontrado.");
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return OperationResult<MeResponse>.Validation(
                PerfilErrorCodes.PasswordAtualIncorreta,
                "Password atual incorreta.");
        }

        if (!PasswordPolicy.IsValid(request.NewPassword))
        {
            return OperationResult<MeResponse>.Validation(
                PerfilErrorCodes.PasswordComplexidadeInsuficiente,
                "Password tem de ter pelo menos 8 caracteres, com maiúscula, minúscula, dígito e símbolo.");
        }

        if (_passwordHasher.Verify(request.NewPassword, user.PasswordHash))
        {
            return OperationResult<MeResponse>.Validation(
                PerfilErrorCodes.PasswordIgualAtual,
                "A nova password tem de ser diferente da atual.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.PasswordTemporaria = false;

        await _auditService.LogAsync(
            AuditAcoes.PasswordAlterada, AuditResultados.Sucesso, utilizadorId, ipAddress,
            targetEntidade: nameof(Utilizador), targetId: utilizadorId,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var lojaDesignacao = await ResolveLojaDesignacaoAsync(user.LojaId, cancellationToken);

        return OperationResult<MeResponse>.Ok(MapMe(user, lojaDesignacao));
    }

    private async Task<string?> ResolveLojaDesignacaoAsync(Guid? lojaId, CancellationToken cancellationToken)
    {
        if (lojaId is null) return null;
        var loja = await _lojaRepository.GetByIdAsync(lojaId.Value, cancellationToken);
        return loja?.Designacao;
    }

    private static MeResponse MapMe(Utilizador u, string? lojaDesignacao) => new()
    {
        Id = u.Id,
        Nome = u.Nome,
        Email = u.Email,
        Perfil = u.Perfil.ToString(),
        LojaId = u.LojaId,
        LojaDesignacao = lojaDesignacao,
        PasswordTemporaria = u.PasswordTemporaria,
        DataCriacao = u.DataCriacao,
        UltimoLogin = u.UltimoLogin
    };
}
