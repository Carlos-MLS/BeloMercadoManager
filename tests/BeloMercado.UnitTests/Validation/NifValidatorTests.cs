using BeloMercado.Application.Common.Validation;

namespace BeloMercado.UnitTests.Validation;

/// <summary>
/// Testes unitários para NifValidator (UC01 RN — emissão de fatura com NIF).
/// Cobre o algoritmo mod-11 conforme regras AT para NIFs portugueses.
/// </summary>
public class NifValidatorTests
{
    // --------- válidos ---------

    [Theory]
    [InlineData("123456789")]   // NIF de teste clássico (check digit 9)
    [InlineData("100000002")]   // pessoa singular (check digit 2)
    [InlineData("111111110")]   // todos 1s (check digit 0)
    [InlineData("200000004")]   // pessoa singular (check digit 4)
    public void IsValid_ReturnsTrue_ForKnownValidNifs(string nif)
    {
        NifValidator.IsValid(nif).Should().BeTrue();
    }

    // --------- comprimento errado ---------

    [Theory]
    [InlineData("")]
    [InlineData("12345678")]      // 8 dígitos
    [InlineData("1234567890")]    // 10 dígitos
    public void IsValid_ReturnsFalse_ForWrongLength(string nif)
    {
        NifValidator.IsValid(nif).Should().BeFalse();
    }

    // --------- não-dígitos ---------

    [Theory]
    [InlineData("12345678A")]
    [InlineData("ABCDEFGHI")]
    [InlineData("12345 789")]
    public void IsValid_ReturnsFalse_WhenNotAllDigits(string nif)
    {
        NifValidator.IsValid(nif).Should().BeFalse();
    }

    // --------- check digit incorreto ---------

    [Theory]
    [InlineData("123456788")]
    [InlineData("123456780")]
    [InlineData("999999999")]
    public void IsValid_ReturnsFalse_ForIncorrectCheckDigit(string nif)
    {
        NifValidator.IsValid(nif).Should().BeFalse();
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForNull()
    {
        NifValidator.IsValid(null!).Should().BeFalse();
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForWhitespace()
    {
        NifValidator.IsValid("         ").Should().BeFalse();
    }
}
