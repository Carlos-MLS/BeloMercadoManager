using BeloMercado.Application.Common.Validation;

namespace BeloMercado.UnitTests.Validation;

/// <summary>
/// Testes unitários para PasswordPolicy (UC17 RN — complexidade obrigatória).
/// Estratégia: equivalence partitioning + boundary value analysis.
/// Categorias: comprimento; classes de caracteres (maiúscula, minúscula,
/// dígito, símbolo); valores nulos / vazios; combinações parciais.
/// </summary>
public class PasswordPolicyTests
{
    // --------- happy path ---------

    [Theory]
    [InlineData("Abc123!@")]     // 8 chars, todas as 4 classes
    [InlineData("Password1!")]   // típica
    [InlineData("XyZ9-aBcd")]    // hífen como símbolo
    [InlineData("Lorem1Ipsum$Dolor")]  // longa
    public void IsValid_ReturnsTrue_ForCompliantPasswords(string password)
    {
        PasswordPolicy.IsValid(password).Should().BeTrue();
    }

    // --------- comprimento ---------

    [Theory]
    [InlineData("")]
    [InlineData("A1!a")]      // 4 chars
    [InlineData("Aa1!aB2")]   // 7 chars — boundary inferior
    public void IsValid_ReturnsFalse_ForTooShortPasswords(string password)
    {
        PasswordPolicy.IsValid(password).Should().BeFalse();
    }

    [Fact]
    public void IsValid_ReturnsTrue_AtBoundary_8Chars()
    {
        PasswordPolicy.IsValid("Aa1!Aa1!").Should().BeTrue();
    }

    // --------- classes em falta ---------

    [Theory]
    [InlineData("abcdefg1!")]    // sem maiúscula
    [InlineData("ABCDEFG1!")]    // sem minúscula
    [InlineData("Abcdefgh!")]    // sem dígito
    [InlineData("Abcdefg1")]     // sem símbolo
    public void IsValid_ReturnsFalse_WhenAnyCategoryIsMissing(string password)
    {
        PasswordPolicy.IsValid(password).Should().BeFalse();
    }

    // --------- null / branco ---------

    [Fact]
    public void IsValid_ReturnsFalse_ForNull()
    {
        PasswordPolicy.IsValid(null!).Should().BeFalse();
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForEmpty()
    {
        PasswordPolicy.IsValid(string.Empty).Should().BeFalse();
    }

    // --------- caracteres não-ASCII contam como símbolo ---------

    [Fact]
    public void IsValid_AcceptsUnicodeSymbol_AsSymbolClass()
    {
        // ç → símbolo na lógica actual (não é letra ASCII)
        // ainda assim, char.IsLower devolve true para 'ç' em pt-PT, logo conta
        // como minúscula. Testamos com símbolo Unicode explícito.
        PasswordPolicy.IsValid("Abcd1€fg").Should().BeTrue();
    }

    [Fact]
    public void MinLength_IsEight()
    {
        PasswordPolicy.MinLength.Should().Be(8);
    }
}
