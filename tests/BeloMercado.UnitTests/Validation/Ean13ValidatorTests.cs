using BeloMercado.Application.Common.Validation;

namespace BeloMercado.UnitTests.Validation;

/// <summary>
/// Testes unitários para Ean13Validator (UC05 RN — código de barras dos produtos).
/// Algoritmo: check digit = (10 - (Σ(d_i * peso_i) mod 10)) mod 10
/// onde peso é 1 nas posições pares (0-based) e 3 nas ímpares.
/// </summary>
public class Ean13ValidatorTests
{
    // --------- válidos ---------

    [Theory]
    [InlineData("5601234567892")]
    [InlineData("4006381333931")]   // EAN-13 conhecido
    [InlineData("9783161484100")]   // ISBN-13
    [InlineData("0000000000000")]   // edge case: tudo zeros é válido
    public void IsValid_ReturnsTrue_ForKnownValidCodes(string code)
    {
        Ean13Validator.IsValid(code).Should().BeTrue();
    }

    // --------- comprimento errado ---------

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("123456789012")]      // 12 dígitos
    [InlineData("12345678901234")]    // 14 dígitos
    public void IsValid_ReturnsFalse_ForWrongLength(string code)
    {
        Ean13Validator.IsValid(code).Should().BeFalse();
    }

    // --------- não-dígitos ---------

    [Theory]
    [InlineData("560123456789A")]
    [InlineData("ABCDEFGHIJKLM")]
    [InlineData("5601234 67892")]
    public void IsValid_ReturnsFalse_WhenContainsNonDigits(string code)
    {
        Ean13Validator.IsValid(code).Should().BeFalse();
    }

    // --------- check digit incorreto ---------

    [Theory]
    [InlineData("5601234567890")]
    [InlineData("5601234567891")]
    [InlineData("5601234567899")]
    public void IsValid_ReturnsFalse_ForIncorrectCheckDigit(string code)
    {
        Ean13Validator.IsValid(code).Should().BeFalse();
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForNull()
    {
        Ean13Validator.IsValid(null!).Should().BeFalse();
    }
}
