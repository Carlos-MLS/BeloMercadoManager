using BeloMercado.Application.Common;

namespace BeloMercado.UnitTests.Common;

/// <summary>
/// Testes para o tipo OperationResult — discriminated union usado por todos
/// os serviços para devolver sucesso, validação, conflito, not-found ou
/// forbidden de forma uniforme. É um wrapper crítico: bug aqui propaga
/// para todos os controllers/UI.
/// </summary>
public class OperationResultTests
{
    [Fact]
    public void Ok_SetsStatusAndValue()
    {
        var r = OperationResult<int>.Ok(42);
        r.Status.Should().Be(OperationStatus.Ok);
        r.Value.Should().Be(42);
        r.IsSuccess.Should().BeTrue();
        r.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void NotFound_SetsCodeAndMessage()
    {
        var r = OperationResult<string>.NotFound("X_NAO_ENCONTRADO", "X não encontrado.");
        r.Status.Should().Be(OperationStatus.NotFound);
        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be("X_NAO_ENCONTRADO");
        r.ErrorMessage.Should().Be("X não encontrado.");
        r.Value.Should().BeNull();
    }

    [Fact]
    public void Conflict_SetsConflictStatus()
    {
        var r = OperationResult<string>.Conflict("CONFLITO", "Já existe.");
        r.Status.Should().Be(OperationStatus.Conflict);
        r.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Validation_SetsValidationStatus()
    {
        var r = OperationResult<string>.Validation("INVALIDO", "Campo X inválido.");
        r.Status.Should().Be(OperationStatus.Validation);
        r.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Forbidden_SetsForbiddenStatus()
    {
        var r = OperationResult<string>.Forbidden("PROIBIDO", "Sem permissão.");
        r.Status.Should().Be(OperationStatus.Forbidden);
        r.IsSuccess.Should().BeFalse();
    }
}
